using System.Text.Json;
using MyApp.AI.Contracts;
using MyApp.Application.DTOs;

namespace MyApp.Web.Models;

public sealed record UiBlockRequest(string Type, JsonElement Payload);

public sealed record RenderedBlockVm(string ViewName, ResponseBlock Model);

public sealed record BotMessageVm(
    string SessionId,
    string Reply,
    IReadOnlyList<RenderedBlockVm> Blocks,
    IReadOnlyList<OrderDto>? UpdatedOrders = null);

public static class UiBlockMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Direct pattern matching without JSON serialization round-trip.
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

    public static RenderedBlockVm Map(string type, JsonElement payload) => type switch
    {
        "ask_input" => Map(Deserialize<AskInputBlock>(payload)),
        "ask_form" => Map(Deserialize<AskFormBlock>(payload)),
        "ask_choice" => Map(Deserialize<AskChoiceBlock>(payload)),
        "table" => Map(Deserialize<TableBlock>(payload)),
        "card" => Map(Deserialize<CardBlock>(payload)),
        "alert" => Map(Deserialize<AlertBlock>(payload)),
        _ => throw new InvalidOperationException($"Unknown block type '{type}'.")
    };

    private static T Deserialize<T>(JsonElement payload) where T : ResponseBlock =>
        payload.Deserialize<T>(JsonOptions)
        ?? throw new InvalidOperationException($"Invalid payload for {typeof(T).Name}.");
}
