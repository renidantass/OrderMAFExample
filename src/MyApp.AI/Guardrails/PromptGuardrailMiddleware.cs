using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyApp.AI.Contracts;

namespace MyApp.AI.Guardrails;

/// <summary>
/// Input/output guardrail middleware delegating policy decisions to IPromptGuard.
/// </summary>
public static class PromptGuardrailMiddleware
{
    public static Func<IEnumerable<ChatMessage>, AgentSession?, AgentRunOptions?, AIAgent, CancellationToken, Task<AgentResponse>>
        Create(IPromptGuard promptGuard, ResponseBlockCollector blockCollector)
    {
        return async (messages, session, options, innerAgent, cancellationToken) =>
        {
            var lastUserText = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? string.Empty;

            var inputDecision = await promptGuard.EvaluateInputAsync(lastUserText, cancellationToken);
            if (!inputDecision.Allowed)
            {
                blockCollector.Add(new AlertBlock(
                    Variant: "warning",
                    Title: "Solicitação bloqueada",
                    Message: inputDecision.Reason ?? "Solicitação não permitida."));

                return new AgentResponse(
                    new ChatMessage(ChatRole.Assistant, "Não posso processar essa solicitação."));
            }

            var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);

            var outputDecision = promptGuard.EvaluateOutput(response.Text ?? string.Empty);
            if (!outputDecision.Allowed)
            {
                blockCollector.Add(new AlertBlock(
                    Variant: "warning",
                    Title: "Resposta filtrada",
                    Message: outputDecision.Reason ?? "Resposta bloqueada pelo guardrail."));

                return new AgentResponse(
                    new ChatMessage(ChatRole.Assistant, "Desculpe, não consegui gerar uma resposta válida. Tente reformular sua pergunta."));
            }

            return response;
        };
    }
}
