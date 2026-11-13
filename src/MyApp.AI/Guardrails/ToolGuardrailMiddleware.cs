using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyApp.AI.Contracts;
using MyApp.AI.Tools;

namespace MyApp.AI.Guardrails;

/// <summary>
/// Deterministic tool guardrail middleware for AIAgent:
/// 1. Prevents infinite tool-calling loops (max calls per turn).
/// 2. Enforces human-in-the-loop confirmation before cancelling an order.
/// </summary>
public sealed class ToolGuardrailMiddleware
{
    private const int MaxToolCallsPerTurn = 5;

    public static Func<AIAgent, FunctionInvocationContext, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>>, CancellationToken, ValueTask<object?>>
        Create(ResponseBlockCollector blockCollector, string currentUserPrompt)
    {
        var toolCallsInTurn = 0;

        return async (agent, context, next, cancellationToken) =>
        {
            toolCallsInTurn++;
            if (toolCallsInTurn > MaxToolCallsPerTurn)
            {
                blockCollector.Add(new AlertBlock(
                    Variant: "warning",
                    Title: "Limite de ações atingido",
                    Message: "O agente atingiu o limite de chamadas de ferramentas neste turno."));

                return "GUARDRAIL_BLOCKED: maximum tool calls per turn reached. Stop calling tools and answer the user.";
            }

            if (string.Equals(context.Function.Name, nameof(OrderTools.CancelOrderAsync), StringComparison.Ordinal))
            {
                var orderId = context.Arguments.TryGetValue("orderId", out var raw)
                    ? raw?.ToString()?.Trim()
                    : null;

                if (!string.IsNullOrWhiteSpace(orderId) && !IsConfirmedInPrompt(currentUserPrompt, orderId))
                {
                    blockCollector.Add(new AskChoiceBlock(
                        Question: $"Confirma o cancelamento do pedido {orderId}?",
                        Options: [$"Confirmar cancelamento {orderId}", "Desistir"]));

                    return $"GUARDRAIL_CONFIRMATION_REQUIRED: explicit user confirmation is required before cancelling '{orderId}'. Confirmation buttons are already queued on UI. Ask the user concisely to confirm.";
                }
            }

            return await next(context, cancellationToken);
        };
    }

    private static bool IsConfirmedInPrompt(string prompt, string orderId)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return false;

        return prompt.Contains($"Confirmar cancelamento {orderId}", StringComparison.OrdinalIgnoreCase)
            || (prompt.Contains("confirmar", StringComparison.OrdinalIgnoreCase) && prompt.Contains(orderId, StringComparison.OrdinalIgnoreCase));
    }
}
