using MyApp.AI.Contracts;
using MyApp.Application.DTOs;

namespace MyApp.Web.Models;

public sealed record RenderedBlockVm(string ViewName, ResponseBlock Model);

public sealed record BotMessageVm(
    string SessionId,
    string Reply,
    IReadOnlyList<RenderedBlockVm> Blocks,
    IReadOnlyList<OrderDto>? UpdatedOrders = null);

public static class UiBlockMapper
{
    public static RenderedBlockVm Map(ResponseBlock block) => block switch
    {
        AskInputBlock => new("~/Views/Shared/UiBlocks/_AskInputBlock.cshtml", block),
        AskFormBlock => new("~/Views/Shared/UiBlocks/_AskFormBlock.cshtml", block),
        AskChoiceBlock => new("~/Views/Shared/UiBlocks/_AskChoiceBlock.cshtml", block),
        TableBlock => new("~/Views/Shared/UiBlocks/_TableBlock.cshtml", block),
        CardBlock => new("~/Views/Shared/UiBlocks/_CardBlock.cshtml", block),
        AlertBlock => new("~/Views/Shared/UiBlocks/_AlertBlock.cshtml", block),
        _ => throw new InvalidOperationException($"Unknown block type '{block.BlockType}'.")
    };
}
