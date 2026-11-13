using System.Text.Json.Serialization;

namespace MyApp.AI.UI;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AskInputBlock), "ask_input")]
[JsonDerivedType(typeof(AskFormBlock), "ask_form")]
[JsonDerivedType(typeof(AskChoiceBlock), "ask_choice")]
[JsonDerivedType(typeof(TableBlock), "table")]
[JsonDerivedType(typeof(CardBlock), "card")]
[JsonDerivedType(typeof(AlertBlock), "alert")]
public abstract record UiBlock(string BlockType);

public sealed record AskInputBlock(
    string FieldName,
    string? Label,
    string? InputType = "text",
    string? Placeholder = null) : UiBlock("ask_input");

public sealed record AskFormField(
    string FieldName,
    string? Label,
    string? InputType = "text",
    string? Placeholder = null);

public sealed record AskFormBlock(
    string? Title,
    string? SubmitLabel,
    IReadOnlyList<AskFormField> Fields) : UiBlock("ask_form");

public sealed record AskChoiceBlock(
    string? Question,
    IReadOnlyList<string> Options) : UiBlock("ask_choice");

public sealed record TableBlock(
    string? Title,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows) : UiBlock("table");

public sealed record CardField(string Label, string Value);

public sealed record CardBlock(
    string? Title,
    string? Badge,
    IReadOnlyList<CardField> Fields) : UiBlock("card");

public sealed record AlertBlock(
    string Variant,
    string? Title,
    string? Message) : UiBlock("alert");

public sealed record AgentTurnResult(
    string SessionId,
    string Reply,
    IReadOnlyList<UiBlock> Blocks,
    bool OrdersMutated);

/// <summary>
/// Scoped collector where UI tools push strongly-typed blocks during an agent turn.
/// </summary>
public sealed class UiBlockCollector
{
    private readonly List<UiBlock> _blocks = [];

    public IReadOnlyList<UiBlock> Blocks => _blocks;
    public bool OrdersMutated { get; private set; }

    public void Add(UiBlock block) => _blocks.Add(block);
    public void MarkOrdersMutated() => OrdersMutated = true;
}
