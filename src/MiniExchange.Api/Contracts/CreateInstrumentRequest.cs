namespace MiniExchange.Api.Contracts;

public sealed record CreateInstrumentRequest(
    string Ticker,
    string Name);