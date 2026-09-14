using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExchange.Application.Outbox;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;

namespace MiniExchange.Infrastructure.Messaging;

public sealed class OutboxPublisher : BackgroundService
{
    private static readonly ActivitySource ActivitySource =
        new("MiniExchange.Infrastructure");

    private static readonly TextMapPropagator Propagator =
        Propagators.DefaultTextMapPropagator;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<string, string> _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IProducer<string, string> producer,
        IOptions<KafkaOptions> options,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var repository =
                    scope.ServiceProvider
                        .GetRequiredService<IOutboxRepository>();

                var messages =
                    await repository.GetUnpublishedAsync(
                        _options.OutboxBatchSize,
                        stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(
                            _options.OutboxPollIntervalMs),
                        stoppingToken);

                    continue;
                }

                foreach (var message in messages)
                {
                    await PublishAsync(
                        repository,
                        message,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Outbox publisher iteration failed.");

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
        }

        _producer.Flush(
            TimeSpan.FromSeconds(5));

        _logger.LogInformation(
            "Outbox publisher stopped.");
    }

    private async Task PublishAsync(
        IOutboxRepository repository,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        ActivityContext parentContext;

        if (!string.IsNullOrWhiteSpace(message.TraceParent) &&
            ActivityContext.TryParse(
                message.TraceParent,
                message.TraceState,
                isRemote: true,
                out var parsedContext))
        {
            parentContext = parsedContext;
        }
        else
        {
            parentContext = default;
        }

        using var activity =
            ActivitySource.StartActivity(
                "Kafka Publish",
                ActivityKind.Producer,
                parentContext);

        activity?.SetTag(
            "messaging.system",
            "kafka");

        activity?.SetTag(
            "messaging.destination.name",
            _options.OrdersTopic);

        activity?.SetTag(
            "messaging.operation.type",
            "publish");

        activity?.SetTag(
            "messaging.message.id",
            message.Id);

        activity?.SetTag(
            "messaging.kafka.message_key",
            message.Key);

        var headers = new Headers();

        var propagationContext =
            new PropagationContext(
                activity?.Context ?? parentContext,
                Baggage.Current);

        Propagator.Inject(
            propagationContext,
            headers,
            static (carrier, key, value) =>
            {
                carrier.Add(
                    key,
                    Encoding.UTF8.GetBytes(value));
            });

        try
        {
            await _producer.ProduceAsync(
                _options.OrdersTopic,
                new Message<string, string>
                {
                    Key = message.Key,
                    Value = message.Payload,
                    Headers = headers
                },
                cancellationToken);

            await repository.MarkPublishedAsync(
                message.Id,
                DateTime.UtcNow,
                cancellationToken);

            activity?.SetStatus(
                ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Published outbox message. " +
                "Id={Id}, Type={Type}, Topic={Topic}, Key={Key}",
                message.Id,
                message.Type,
                _options.OrdersTopic,
                message.Key);
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
}