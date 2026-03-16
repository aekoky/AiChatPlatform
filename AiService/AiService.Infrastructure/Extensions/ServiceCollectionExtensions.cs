using AiService.Infrastructure.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;

namespace AiService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOllamaLlmClient(this IServiceCollection services)
    {
        services.AddChatClient(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaApiClient(options.BaseUrl, options.Model);
        });

        return services;
    }

    public static IServiceCollection AddOpenAILlmClient(this IServiceCollection services)
    {
        services.AddChatClient(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            var openAIClient = new OpenAIClient(options.ApiKey);

            return openAIClient
                  .GetChatClient(options.Model)
                  .AsIChatClient();
        });

        return services;
    }
}
