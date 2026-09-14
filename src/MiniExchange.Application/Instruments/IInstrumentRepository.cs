namespace MiniExchange.Application.Instruments;

public interface IInstrumentRepository
{
    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken);
}