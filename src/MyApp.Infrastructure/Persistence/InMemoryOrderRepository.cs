using System.Collections.Concurrent;
using MyApp.Application.Abstractions;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Persistence;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<string, Order> _orders = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryOrderRepository()
    {
        Seed(new Order("ORD-1001", "Reni", 249.90m, OrderStatus.Confirmed));
        Seed(new Order("ORD-1002", "Maria", 89.50m, OrderStatus.Shipped));
        Seed(new Order("ORD-1003", "Carlos", 520.00m, OrderStatus.Pending));
    }

    private void Seed(Order order) => _orders[order.Id] = order;

    public Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<Order>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Order> result = _orders.Values.OrderBy(o => o.Id).ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
