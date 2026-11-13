using System.Collections.Concurrent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MyApp.AI.Contracts;
using MyApp.AI.Tools;

namespace MyApp.AI.Agents;

public interface IOrderAssistant
{
    Task<AgentTurnResult> AskAsync(string prompt, string? sessionId = null, CancellationToken cancellationToken = default);
}

public sealed class OrderAssistantAgent(
    IChatClient chatClient,
    OrderTools orderTools,
    InteractionTools interactionTools,
    ResponseBlockCollector blockCollector) : IOrderAssistant
{
    private static readonly ConcurrentDictionary<string, AgentSession> Sessions = new();

    private const string SystemInstructions =
        """
        Você é um assistente de pedidos em português.
        Regras obrigatórias:
        1. NUNCA invente pedidos, IDs, clientes ou valores. Para saber os pedidos atuais, SEMPRE chame ListOrdersAsync antes de responder.
        2. NUNCA diga que um pedido foi criado ou cancelado sem antes chamar CreateOrderAsync ou CancelOrderAsync e receber o resultado da ferramenta.
        3. NUNCA escreva nomes de ferramentas (como RenderTable, AskUserInput) nem JSON na sua resposta ao usuário. Use tool calling de verdade.
        4. Quando o usuário quiser criar um pedido, SEMPRE chame CreateOrderAsync imediatamente (mesmo sem nome ou valor — a ferramenta exibirá o formulário na tela automaticamente).
        5. As ferramentas de pedidos já renderizam tabelas, cards e alertas visuais automaticamente na tela. NÃO use markdown (tabelas, listas com - ou *, negrito **) na resposta — responda sempre com uma frase curta e simples.
        6. Ao precisar de confirmação do usuário, chame AskUserChoice.
        """;

    public async Task<AgentTurnResult> AskAsync(
        string prompt,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var activeSessionId = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")[..12]
            : sessionId;

        AITool[] allTools = [.. orderTools.AsAITools(), .. interactionTools.AsAITools()];

        AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "OrderAssistant",
            ChatOptions = new ChatOptions
            {
                Instructions = SystemInstructions,
                Temperature = 0.1f,
                Tools = allTools
            }
        });

        if (!Sessions.TryGetValue(activeSessionId, out var session))
        {
            session = await agent.CreateSessionAsync(cancellationToken);
            Sessions[activeSessionId] = session;
        }

        var response = await agent.RunAsync(prompt, session, cancellationToken: cancellationToken);
        var replyText = response.Text ?? string.Empty;

        return new AgentTurnResult(
            activeSessionId,
            replyText,
            blockCollector.Blocks.ToList(),
            blockCollector.OrdersMutated);
    }
}
