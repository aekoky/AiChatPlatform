using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace AiService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddChatClient(new OllamaApiClient(configuration["Ollama:BaseUrl"]!, configuration["Ollama:Model"]!));

        return services;
    }
}
