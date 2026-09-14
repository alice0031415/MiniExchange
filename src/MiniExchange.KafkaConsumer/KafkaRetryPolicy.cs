namespace MiniExchange.KafkaConsumer;

public sealed class KafkaRetryPolicy
{
    private readonly int _maxAttempts;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public KafkaRetryPolicy(
        int maxAttempts = 3,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        if (maxAttempts <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts));

        _maxAttempts = maxAttempts;
        _delay = delay ?? Task.Delay;
    }

    public async Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1;
             attempt <= _maxAttempts;
             attempt++)
        {
            try
            {
                await action();

                return;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == _maxAttempts)
                    break;

                var delay = TimeSpan.FromSeconds(
                    Math.Pow(2, attempt - 1));

                await _delay(
                    delay,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {_maxAttempts} attempts.",
            lastException);
    }
}