using Grpc.Core;
using MiniExchange.Contracts.Grpc;

namespace MiniExchange.ReadApi.Services;

public sealed class OrderQueryClient
{
    private readonly OrderQuery.OrderQueryClient _client;

    public OrderQueryClient(
        OrderQuery.OrderQueryClient client)
    {
        _client = client;
    }

    public async Task<GetOrdersResponse> GetOrdersAsync(
        Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var request = new GetOrdersRequest();

        if (instrumentId.HasValue)
        {
            request.InstrumentId =
                instrumentId.Value.ToString();
        }

        try
        {
            return await _client.GetOrdersAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(3),
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
            when (ex.StatusCode is
                StatusCode.Unavailable or
                StatusCode.DeadlineExceeded)
        {
            throw new OrderQueryUnavailableException(
                "Order query service is unavailable.",
                ex);
        }
    }

    public async Task<GetTradesResponse> GetTradesAsync(
        Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var request = new GetTradesRequest();

        if (instrumentId.HasValue)
        {
            request.InstrumentId =
                instrumentId.Value.ToString();
        }

        try
        {
            return await _client.GetTradesAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(3),
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
            when (ex.StatusCode is
                StatusCode.Unavailable or
                StatusCode.DeadlineExceeded)
        {
            throw new OrderQueryUnavailableException(
                "Order query service is unavailable.",
                ex);
        }
    }
}