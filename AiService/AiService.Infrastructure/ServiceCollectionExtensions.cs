using AiService.Application.Handlers;
using AiService.Application.Services;
using AiService.Infrastructure.Options;
using AiService.Infrastructure.Persistence;
using AiService.Infrastructure.Services;
using BuildingBlocks.Contracts.LlmEvents;
using BuildingBlocks.Contracts.SessionEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;
using Wolverine;
using Wolverine.RabbitMQ;

namespace AiService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOllamaLlmClient(this IServiceCollection services)
    {
        services.AddHttpClient("OllamaChat", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddChatClient(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("OllamaChat");
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;

            return new OllamaApiClient(httpClient, options.Model);
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

    public static IServiceCollection AddRagRetrieval(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiPromptOptions>(configuration.GetSection(AiPromptOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is missing.");

        services.AddDbContext<AiDbContext>(opts =>
            opts.UseNpgsql(connectionString, o => o.UseVector()));

        services.AddSingleton<LlmCancellationRegistry>();

        services.AddHttpClient("OllamaEmbedding", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddEmbeddingGenerator(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("OllamaEmbedding");
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;

            return new OllamaApiClient(httpClient, options.EmbeddingModel);
        });

        services.AddScoped<IRagRetrievalService, PgVectorRetrievalService>();
        services.AddScoped<IRagTool, RagTool>();
        services.AddScoped<ILlmService, OllamaLlmService>();

        return services;
    }


    public static void ConfigureWolverine(this WolverineOptions opts, IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var rabbitOptions = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        opts.Discovery.IncludeAssembly(typeof(GenerateAiResponseHandler).Assembly);
        // Set a longer timeout for LLM operations, as they can take time to complete
        opts.DefaultExecutionTimeout = TimeSpan.FromSeconds(300);

        opts.Policies.UseDurableLocalQueues();

        opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

        opts.ListenToRabbitQueue("llm-requests");

        opts.ListenToRabbitQueue("llm-summarization")
            .Sequential();

        opts.PublishMessage<LlmTokensGeneratedEvent>()
            .ToRabbitQueue("llm-tokens")
            .SendInline(); // Stream immediately, do not wait for the outbox to clear

        opts.PublishMessage<LlmSourcesFoundEvent>()
            .ToRabbitQueue("llm-sources");

        opts.PublishMessage<LlmResponseRetryingEvent>()
            .ToRabbitQueue("llm-retrying")
            .SendInline();

        opts.PublishMessage<SessionSummaryGeneratedEvent>()
            .ToRabbitQueue("summary-tokens");

        opts.PublishMessage<LlmResponseCompletedEvent>()
            .ToRabbitExchange("llm-completed");

        opts.PublishMessage<LlmResponseGaveUpEvent>()
            .ToRabbitExchange("llm-gave-up");
    }
}
