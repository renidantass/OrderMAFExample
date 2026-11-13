using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.AI.Agents;
using MyApp.AI.Contracts;
using MyApp.AI.Guardrails;
using MyApp.AI.Tools;
using OllamaSharp;

namespace MyApp.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAI(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["AI:Ollama:Endpoint"] ?? "http://localhost:11434";
        var model = configuration["AI:Ollama:Model"] ?? "qwen3:8b";

        services.AddSingleton<IChatClient>(_ => new OllamaApiClient(new Uri(endpoint), model));
        services.AddSingleton(_ => GuardrailOptions.FromConfiguration(configuration));
        services.AddSingleton<IPromptGuard, DefaultPromptGuard>();

        services.AddScoped<ResponseBlockCollector>();
        services.AddScoped<OrderTools>();
        services.AddScoped<InteractionTools>();
        services.AddScoped<IOrderAssistant, OrderAssistantAgent>();

        return services;
    }
}
