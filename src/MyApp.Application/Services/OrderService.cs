using MyApp.Application.Abstractions;
using MyApp.Application.DTOs;
using MyApp.Domain.Entities;

namespace MyApp.Application.Services;

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> CancelAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class OrderService(IOrderRepository repository) : IOrderService
{
    public async Task<OrderDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken);
        return order is null ? null : Map(order);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var orders = await repository.ListAsync(cancellationToken);
        return orders.Select(Map).ToList();
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var id = $"ORD-{Random.Shared.Next(1000, 9999)}";
        var order = new Order(id, request.CustomerName, request.Total);
        await repository.AddAsync(order, cancellationToken);
        return Map(order);
    }

    public async Task<OrderDto> CancelAsync(string id, CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Order '{id}' not found.");

        order.Cancel();
        await repository.UpdateAsync(order, cancellationToken);
        return Map(order);
    }

    private static OrderDto Map(Order order) =>
        new(order.Id, order.CustomerName, order.Total, order.Status.ToString(), order.CreatedAt);
}
