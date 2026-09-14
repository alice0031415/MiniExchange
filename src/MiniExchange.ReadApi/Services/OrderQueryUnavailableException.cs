namespace MiniExchange.ReadApi.Services;

public sealed class OrderQueryUnavailableException
    : Exception
{
    public OrderQueryUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}