using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniExchange.Contracts.Events;
using OpenTelemetry.Context.Propagation;
using MiniExchange.KafkaConsumer.Metrics;

namespace MiniExchange.KafkaConsumer;

public sealed class KafkaConsumerWorker : BackgroundService
{
    private static readonly ActivitySource ActivitySource =
        new("MiniExchange.KafkaConsumer");

    private static readonly TextMapPropagator Propagator =
        Propagators.DefaultTextMapPropagator;

    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly KafkaRetryPolicy _retryPolicy;
    private readonly IProducer<string, string> _producer;

    public KafkaConsumerWorker(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumerWorker> logger,
        IProducer<string, string> producer)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _producer = producer;
        _retryPolicy = new KafkaRetryPolicy();
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer =
            new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(_options.Topic);

        _logger.LogInformation(
            "Kafka consumer started. " +
            "Topic={Topic}, GroupId={GroupId}",
            _options.Topic,
            _options.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    await ProcessWithRetryAsync(
                        consumer,
                        result,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(
                        ex,
                        "Kafka consume error.");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Invalid Kafka message. " +
                        "Offset will not be committed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Message processing failed after retries. " +
                        "Offset will not be committed.");
                }
            }
        }
        finally
        {
            consumer.Close();
            _producer.Flush(TimeSpan.FromSeconds(5));

            _logger.LogInformation(
                "Kafka consumer stopped.");
        }
    }

    private async Task ProcessWithRetryAsync(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        try
        {
            var attempts = 0;

            await _retryPolicy.ExecuteAsync(
                async () =>
                {
                    attempts++;

                    if (attempts > 1)
                    {
                        KafkaConsumerMetrics.MessagesRetried.Inc();
                    }

                    _logger.LogInformation(
                        "Processing {TopicPartitionOffset}. " +
                        "Attempt={Attempt}",
                        result.TopicPartitionOffset,
                        attempts);

                    await ProcessMessageAsync(
                        result,
                        cancellationToken);
                },
                cancellationToken);

            consumer.Commit(result);

            _logger.LogInformation(
                "Committed {TopicPartitionOffset}. " +
                "Attempts={Attempts}",
                result.TopicPartitionOffset,
                attempts);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await PublishToDeadLetterAsync(
                result,
                ex,
                cancellationToken);

            KafkaConsumerMetrics.MessagesDeadLettered.Inc();

            consumer.Commit(result);

            _logger.LogWarning(
                "Committed {TopicPartitionOffset} " +
                "after DLQ publish.",
                result.TopicPartitionOffset);
        }
    }

    private async Task ProcessMessageAsync(
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        var envelope =
            JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(
                result.Message.Value);

        if (envelope is null)
        {
            throw new JsonException(
                "Failed to deserialize event envelope.");
        }

        var parentContext =
            Propagator.Extract(
                default,
                result.Message.Headers,
                static (headers, key) =>
                {
                    var header = headers
                        .LastOrDefault(
                            x => string.Equals(
                                x.Key,
                                key,
                                StringComparison.OrdinalIgnoreCase));

                    return header?.GetValueBytes() is { } bytes
                        ? new[]
                        {
                            Encoding.UTF8.GetString(bytes)
                        }
                        : Array.Empty<string>();
                });

        using var activity =
            ActivitySource.StartActivity(
                "ProcessKafkaEvent",
                ActivityKind.Consumer,
                parentContext.ActivityContext);

        activity?.SetTag(
            "messaging.system",
            "kafka");

        activity?.SetTag(
            "messaging.destination.name",
            _options.Topic);

        activity?.SetTag(
            "messaging.kafka.partition",
            result.Partition.Value);

        activity?.SetTag(
            "messaging.kafka.offset",
            result.Offset.Value);

        activity?.SetTag(
            "messaging.event.type",
            envelope.EventType);

        activity?.SetTag(
            "messaging.event.id",
            envelope.EventId);

        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            switch (envelope.EventType)
            {
                case nameof(OrderSubmitted):
                    {
                        var handler =
                            scope.ServiceProvider
                                .GetRequiredService<IOrderSubmittedHandler>();

                        var message =
                            JsonSerializer.Deserialize<
                                EventEnvelope<OrderSubmitted>>(
                                result.Message.Value);

                        if (message is null)
                        {
                            throw new JsonException(
                                "Failed to deserialize OrderSubmitted.");
                        }

                        await handler.HandleAsync(
                            message,
                            cancellationToken);

                        break;
                    }

                case nameof(OrderStateChanged):
                    {
                        var handler =
                            scope.ServiceProvider
                                .GetRequiredService<IOrderStateChangedHandler>();

                        var message =
                            JsonSerializer.Deserialize<
                                EventEnvelope<OrderStateChanged>>(
                                result.Message.Value);

                        if (message is null)
                        {
                            throw new JsonException(
                                "Failed to deserialize OrderStateChanged.");
                        }

                        await handler.HandleAsync(
                            message,
                            cancellationToken);

                        break;
                    }

                case nameof(TradeExecuted):
                    {
                        var handler =
                            scope.ServiceProvider
                                .GetRequiredService<ITradeExecutedHandler>();

                        var message =
                            JsonSerializer.Deserialize<
                                EventEnvelope<TradeExecuted>>(
                                result.Message.Value);

                        if (message is null)
                        {
                            throw new JsonException(
                                "Failed to deserialize TradeExecuted.");
                        }

                        await handler.HandleAsync(
                            message,
                            cancellationToken);

                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        $"Unknown event type '{envelope.EventType}'.");
            }

            activity?.SetStatus(
                ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetTag(
                "error.type",
                ex.GetType().FullName);

            activity?.SetTag(
                "error.message",
                ex.Message);

            activity?.SetStatus(
                ActivityStatusCode.Error);

            throw;
        }
    }

    private async Task PublishToDeadLetterAsync(
        ConsumeResult<string, string> result,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var headers = new Headers();

        headers.Add(
            "x-original-topic",
            Encoding.UTF8.GetBytes(result.Topic));

        headers.Add(
            "x-original-partition",
            Encoding.UTF8.GetBytes(
                result.Partition.Value.ToString()));

        headers.Add(
            "x-original-offset",
            Encoding.UTF8.GetBytes(
                result.Offset.Value.ToString()));

        headers.Add(
            "x-error-type",
            Encoding.UTF8.GetBytes(
                exception.GetType().FullName
                ?? exception.GetType().Name));

        headers.Add(
            "x-error-message",
            Encoding.UTF8.GetBytes(
                exception.Message));

        await _producer.ProduceAsync(
            _options.DeadLetterTopic,
            new Message<string, string>
            {
                Key = result.Message.Key,
                Value = result.Message.Value,
                Headers = headers
            },
            cancellationToken);

        _logger.LogError(
            exception,
            "Message moved to DLQ. " +
            "Source={TopicPartitionOffset}, " +
            "DLQ={DeadLetterTopic}",
            result.TopicPartitionOffset,
            _options.DeadLetterTopic);
    }
}