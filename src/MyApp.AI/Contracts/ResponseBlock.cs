using System.Text.Json.Serialization;

namespace MyApp.AI.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AskInputBlock), "ask_input")]
[JsonDerivedType(typeof(AskFormBlock), "ask_form")]
[JsonDerivedType(typeof(AskChoiceBlock), "ask_choice")]
[JsonDerivedType(typeof(TableBlock), "table")]
[JsonDerivedType(typeof(CardBlock), "card")]
[JsonDerivedType(typeof(AlertBlock), "alert")]
public abstract record ResponseBlock(string BlockType);

public sealed record AskInputBlock(
    string FieldName,
    string? Label,
    string? InputType = "text",
    string? Placeholder = null) : ResponseBlock("ask_input");

public sealed record AskFormField(
    string FieldName,
    string? Label,
    string? InputType = "text",
    string? Placeholder = null);

public sealed record AskFormBlock(
    string? Title,
    string? SubmitLabel,
    IReadOnlyList<AskFormField> Fields) : ResponseBlock("ask_form");

public sealed record AskChoiceBlock(
    string? Question,
    IReadOnlyList<string> Options) : ResponseBlock("ask_choice");

public sealed record TableBlock(
    string? Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows) : ResponseBlock("table");

public sealed record CardField(string Label, string Value);

public sealed record CardBlock(
    string? Title,
    string? Badge,
    IReadOnlyList<CardField> Fields) : ResponseBlock("card");

public sealed record AlertBlock(
    string Variant,
    string? Title,
    string? Message) : ResponseBlock("alert");

public sealed record AgentTurnResult(
    string SessionId,
    string Reply,
    IReadOnlyList<ResponseBlock> Blocks,
    bool OrdersMutated);

/// <summary>
/// Scoped collector where agent tools push strongly-typed response blocks during a turn.
/// </summary>
public sealed class ResponseBlockCollector
{
    private readonly List<ResponseBlock> _blocks = [];

    public IReadOnlyList<ResponseBlock> Blocks => _blocks;
    public bool OrdersMutated { get; private set; }

    public void Add(ResponseBlock block) => _blocks.Add(block);
    public void MarkOrdersMutated() => OrdersMutated = true;
}
