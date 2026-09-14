using MiniExchange.KafkaConsumer;

namespace MiniExchange.Tests.Consumer;

public sealed class KafkaRetryPolicyTests
{
    private static KafkaRetryPolicy CreatePolicy()
    {
        return new KafkaRetryPolicy(
            maxAttempts: 3,
            delay: (_, _) => Task.CompletedTask);
    }

    [Fact]
    public async Task ExecuteAsync_SucceedsOnFirstAttempt()
    {
        var policy = CreatePolicy();

        var attempts = 0;

        await policy.ExecuteAsync(
            () =>
            {
                attempts++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesAndEventuallySucceeds()
    {
        var policy = CreatePolicy();

        var attempts = 0;

        await policy.ExecuteAsync(
            () =>
            {
                attempts++;

                if (attempts < 3)
                    throw new InvalidOperationException();

                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsAfterMaxAttempts()
    {
        var policy = CreatePolicy();

        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => policy.ExecuteAsync(
                () =>
                {
                    attempts++;
                    throw new InvalidOperationException();
                },
                CancellationToken.None));

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryCancellation()
    {
        var policy = CreatePolicy();

        using var cts = new CancellationTokenSource();

        cts.Cancel();

        var attempts = 0;

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => policy.ExecuteAsync(
                () =>
                {
                    attempts++;

                    throw new OperationCanceledException(
                        cts.Token);
                },
                cts.Token));

        Assert.Equal(1, attempts);
    }
}