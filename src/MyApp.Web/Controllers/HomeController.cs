using Microsoft.AspNetCore.Mvc;
using MyApp.AI.Agents;
using MyApp.Application.Services;
using MyApp.Web.Models;

namespace MyApp.Web.Controllers;

public sealed class HomeController(IOrderService orderService, IOrderAssistant assistant) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var orders = await orderService.ListAsync(ct);
        return View(orders);
    }

    [HttpGet("/orders/partial")]
    public async Task<IActionResult> OrdersPartial(CancellationToken ct)
    {
        var orders = await orderService.ListAsync(ct);
        return PartialView("~/Views/Shared/_OrdersList.cshtml", orders);
    }

    [HttpPost("/chat/turn")]
    public async Task<IActionResult> ChatTurn([FromBody] ChatTurnRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var result = await assistant.AskAsync(request.Message, request.SessionId, ct);

        var hasVisualBlock = result.Blocks.Any(b => b is MyApp.AI.Contracts.TableBlock or MyApp.AI.Contracts.CardBlock or MyApp.AI.Contracts.AlertBlock);
        var reply = hasVisualBlock ? StripMarkdownTablesAndLists(result.Reply) : result.Reply;

        var updatedOrders = result.OrdersMutated
            ? await orderService.ListAsync(ct)
            : null;

        var renderedBlocks = result.Blocks.Select(UiBlockMapper.Map).ToList();
        return PartialView(
            "~/Views/Shared/_BotMessage.cshtml",
            new BotMessageVm(result.SessionId, reply, renderedBlocks, updatedOrders));
    }

    private static string StripMarkdownTablesAndLists(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var lines = text.Split('\n')
            .Where(l => !l.TrimStart().StartsWith('|') && !l.TrimStart().StartsWith("- ") && !l.TrimStart().StartsWith("* "));

        return string.Join('\n', lines).Trim();
    }
}

public sealed record ChatTurnRequest(string Message, string? SessionId = null);
