using MyApp.AI;
using MyApp.AI.Agents;
using MyApp.Application;
using MyApp.Application.DTOs;
using MyApp.Application.Services;
using MyApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddAI(builder.Configuration);

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Traditional endpoints -> WebAPI -> Application
app.MapGet("/orders", async (IOrderService orders, CancellationToken ct) =>
    Results.Ok(await orders.ListAsync(ct)));

app.MapGet("/orders/{id}", async (string id, IOrderService orders, CancellationToken ct) =>
{
    var order = await orders.GetByIdAsync(id, ct);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapPost("/orders", async (CreateOrderRequest request, IOrderService orders, CancellationToken ct) =>
{
    var created = await orders.CreateAsync(request, ct);
    return Results.Created($"/orders/{created.Id}", created);
});

app.MapPost("/orders/{id}/cancel", async (string id, IOrderService orders, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await orders.CancelAsync(id, ct));
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message });
    }
});

// AI Agent endpoint -> WebAPI -> MyApp.AI -> (Tools) -> Application
app.MapPost("/agent/orders", async (AgentPromptRequest request, IOrderAssistant assistant, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest(new { error = "Message is required." });

    try
    {
        var result = await assistant.AskAsync(request.Message, request.SessionId, ct);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Erro ao executar o agente",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

public sealed record AgentPromptRequest(string Message, string? SessionId = null);
