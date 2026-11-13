using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;
using MyApp.AI.Contracts;
using MyApp.Application.DTOs;
using MyApp.Application.Services;

namespace MyApp.AI.Tools;

/// <summary>
/// Thin adapter exposing Application use cases as MAF tools.
/// Automatically queues strongly-typed ResponseBlocks deterministically.
/// </summary>
public sealed class OrderTools(IOrderService orderService, ResponseBlockCollector blockCollector)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(ListOrdersAsync),
        AIFunctionFactory.Create(GetOrderAsync),
        AIFunctionFactory.Create(CreateOrderAsync),
        AIFunctionFactory.Create(CancelOrderAsync)
    ];

    [Description("Lista todos os pedidos cadastrados no sistema e exibe automaticamente em tabela visual na interface.")]
    public async Task<string> ListOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await orderService.ListAsync(cancellationToken);

        var rows = orders
            .Select(o => (IReadOnlyList<string>)
            [
                o.Id,
                o.CustomerName,
                o.Total.ToString("C2", PtBr),
                o.Status,
                o.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            ])
            .ToList();

        blockCollector.Add(new TableBlock(
            Title: $"Pedidos ({orders.Count})",
            Columns: ["Código", "Cliente", "Total", "Status", "Criado em"],
            Rows: rows));

        var dataJson = JsonSerializer.Serialize(orders);
        return $"OK: visual table with {orders.Count} order(s) already rendered on UI. Reply in one short plain sentence without markdown or lists. Data for reference: {dataJson}";
    }

    [Description("Busca os detalhes de um pedido pelo código (ex: ORD-1001) e exibe um card visual na interface.")]
    public async Task<string> GetOrderAsync(
        [Description("Código do pedido, ex: ORD-1001")] string orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderService.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            blockCollector.Add(new AlertBlock(
                Variant: "warning",
                Title: "Pedido não encontrado",
                Message: $"Não existe nenhum pedido com o código '{orderId}'."));

            return $"OK: warning alert rendered on UI for missing order '{orderId}'. Reply in one short plain sentence.";
        }

        QueueOrderCard($"Pedido {order.Id}", order);
        return $"OK: visual card rendered on UI for order {order.Id}. Reply in one short plain sentence without repeating fields or markdown.";
    }

    [Description("Cria um novo pedido. Pode ser chamada mesmo se o usuário ainda não informou nome do cliente ou valor; os campos faltantes serão solicitados automaticamente na interface.")]
    public async Task<string> CreateOrderAsync(
        [Description("Nome do cliente (opcional se ainda não informado)")] string? customerName = null,
        [Description("Valor total do pedido maior que zero (opcional se ainda não informado)")] decimal? total = null,
        CancellationToken cancellationToken = default)
    {
        var missingFields = new List<AskFormField>();

        if (string.IsNullOrWhiteSpace(customerName))
        {
            missingFields.Add(new AskFormField("customerName", "Nome do cliente", "text", "Ex: Ana Silva"));
        }

        if (total is null or <= 0)
        {
            missingFields.Add(new AskFormField("total", "Valor total (R$)", "number", "Ex: 150.50"));
        }

        if (missingFields.Count > 0)
        {
            blockCollector.Add(new AskFormBlock("Novo Pedido", "Criar pedido", missingFields));
            return "OK: form queued on UI for missing fields. Reply with one short plain sentence telling the user to fill in the form.";
        }

        var created = await orderService.CreateAsync(
            new CreateOrderRequest(customerName!, total!.Value),
            cancellationToken);

        blockCollector.MarkOrdersMutated();
        QueueOrderCard($"Pedido criado: {created.Id}", created);
        return $"OK: order {created.Id} created and visual card rendered on UI. Reply in one short plain sentence without markdown.";
    }

    [Description("Cancela um pedido existente pelo código, caso ainda não tenha sido enviado.")]
    public async Task<string> CancelOrderAsync(
        [Description("Código do pedido, ex: ORD-1003")] string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            blockCollector.Add(new AskInputBlock("orderId", "Código do pedido para cancelar", "text", "Ex: ORD-1003"));
            return "OK: input 'orderId' queued on UI. Reply concisely asking the user to enter the order code.";
        }

        try
        {
            var cancelled = await orderService.CancelAsync(orderId, cancellationToken);
            blockCollector.MarkOrdersMutated();
            QueueOrderCard($"Pedido cancelado: {cancelled.Id}", cancelled);
            return $"OK: order {cancelled.Id} cancelled and visual card rendered on UI. Reply in one short plain sentence without markdown.";
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            blockCollector.Add(new AlertBlock("error", $"Não foi possível cancelar {orderId}", ex.Message));
            return $"OK: error alert rendered on UI explaining why '{orderId}' could not be cancelled ({ex.Message}). Reply in one short plain sentence.";
        }
    }

    private void QueueOrderCard(string title, OrderDto order)
    {
        blockCollector.Add(new CardBlock(
            Title: title,
            Badge: order.Status,
            Fields:
            [
                new("Código", order.Id),
                new("Cliente", order.CustomerName),
                new("Total", order.Total.ToString("C2", PtBr)),
                new("Status", order.Status)
            ]));
    }
}
