namespace MyApp.Domain.Entities;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Cancelled
}

public sealed class Order
{
    public string Id { get; }
    public string CustomerName { get; }
    public decimal Total { get; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public Order(string id, string customerName, decimal total, OrderStatus status = OrderStatus.Pending)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Order id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        if (total <= 0)
            throw new ArgumentOutOfRangeException(nameof(total), "Total must be greater than zero.");

        Id = id;
        CustomerName = customerName;
        Total = total;
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException($"Order {Id} has already been shipped and cannot be cancelled.");

        Status = OrderStatus.Cancelled;
    }
}
