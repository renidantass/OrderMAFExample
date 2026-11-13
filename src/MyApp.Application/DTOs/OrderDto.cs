namespace MyApp.Application.DTOs;

public sealed record OrderDto(
    string Id,
    string CustomerName,
    decimal Total,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record CreateOrderRequest(string CustomerName, decimal Total);
