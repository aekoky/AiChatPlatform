using AiService.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace AiService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmClient(this IServiceCollection services)
    {
        services.AddChatClient(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaApiClient(options.BaseUrl, options.Model);
        });

        return services;
    }
}
