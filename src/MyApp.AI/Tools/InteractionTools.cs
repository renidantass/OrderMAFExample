using System.ComponentModel;
using Microsoft.Extensions.AI;
using MyApp.AI.Contracts;

namespace MyApp.AI.Tools;

/// <summary>
/// Generic interaction tools allowing any agent to request structured inputs, choices or visual elements.
/// </summary>
public sealed class InteractionTools(ResponseBlockCollector collector)
{
    public IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(AskUserInput),
        AIFunctionFactory.Create(AskUserChoice),
        AIFunctionFactory.Create(RenderTable),
        AIFunctionFactory.Create(RenderAlert)
    ];

    [Description("Solicita ao usuário que preencha um campo de entrada na interface (ex: texto, número, data). Use quando precisar coletar um dado faltante do usuário.")]
    public string AskUserInput(
        [Description("Nome técnico do campo, ex: customerName ou total")] string fieldName,
        [Description("Rótulo amigável exibido ao usuário, ex: Nome do cliente")] string label,
        [Description("Tipo do input HTML: text, number, email ou date")] string inputType = "text",
        [Description("Texto de exemplo opcional dentro do campo")] string? placeholder = null)
    {
        collector.Add(new AskInputBlock(fieldName, label, inputType, placeholder));
        return $"OK: input '{fieldName}' queued on UI. Ask the user concisely to fill it in. Do NOT repeat JSON or function names.";
    }

    [Description("Exibe botões de múltipla escolha ou confirmação na interface para o usuário clicar.")]
    public string AskUserChoice(
        [Description("Pergunta curta exibida acima dos botões")] string question,
        [Description("Lista de opções clicáveis, ex: [\"Sim\", \"Não\"]")] string[] options)
    {
        collector.Add(new AskChoiceBlock(question, options));
        return "OK: choice buttons queued on UI. Ask the user concisely to pick an option.";
    }

    [Description("Renderiza uma tabela visual na interface para exibir listas ou dados estruturados.")]
    public string RenderTable(
        [Description("Título da tabela")] string title,
        [Description("Nomes das colunas")] string[] columns,
        [Description("Linhas da tabela (cada linha é um array de strings na mesma ordem das colunas)")] string[][] rows)
    {
        collector.Add(new TableBlock(title, columns, rows));
        return $"OK: UI table '{title}' with {rows.Length} row(s) is already visible to the user. IMPORTANT: Do NOT write a markdown table or repeat any rows in your text response. Just write one short sentence (e.g. 'Aqui estão os pedidos:').";
    }

    [Description("Exibe um alerta visual na interface (success, info, warning ou error).")]
    public string RenderAlert(
        [Description("Título curto do alerta")] string title,
        [Description("Mensagem explicativa")] string message,
        [Description("Variante visual: success, info, warning ou error")] string variant = "info")
    {
        collector.Add(new AlertBlock(variant, title, message));
        return $"OK: alert '{title}' rendered on UI. Reply in one short plain sentence.";
    }
}
