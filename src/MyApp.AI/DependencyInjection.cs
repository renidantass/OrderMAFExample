using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.AI.Agents;
using MyApp.AI.Tools;
using MyApp.AI.UI;
using OllamaSharp;

namespace MyApp.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAI(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["AI:Ollama:Endpoint"] ?? "http://localhost:11434";
        var model = configuration["AI:Ollama:Model"] ?? "qwen3:8b";

        services.AddSingleton<IChatClient>(_ => new OllamaApiClient(new Uri(endpoint), model));
        services.AddScoped<UiBlockCollector>();
        services.AddScoped<OrderTools>();
        services.AddScoped<UiTools>();
        services.AddScoped<IOrderAssistant, OrderAssistantAgent>();

        return services;
    }
}
