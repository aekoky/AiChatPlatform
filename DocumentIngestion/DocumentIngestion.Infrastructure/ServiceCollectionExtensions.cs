using Amazon.Runtime;
using Amazon.S3;
using BuildingBlocks.Contracts.DocumentEvents;
using DocumentIngestion.Application.Handlers;
using DocumentIngestion.Application.Services;
using DocumentIngestion.Infrastructure;
using DocumentIngestion.Infrastructure.Options;
using DocumentIngestion.Infrastructure.Parsers;
using DocumentIngestion.Infrastructure.Persistence;
using DocumentIngestion.Infrastructure.Repositories;
using DocumentIngestion.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using Wolverine;
using Wolverine.RabbitMQ;

namespace DocumentIngestion.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIngestionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<KreuzbergParserOptions>(configuration.GetSection(KreuzbergParserOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddS3Client(configuration);
        services.AddEfCore(configuration);
        services.AddEmbeddingGenerator(configuration);

        services.AddScoped<IChunkingService, KreuzbergDocumentParser>();
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IVectorStoreRepository, PgVectorRepository>();

        return services;
    }

    private static IServiceCollection AddS3Client(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration.GetSection(S3Options.SectionName).Get<S3Options>()
            ?? throw new InvalidOperationException("S3 options are missing.");

        if (string.IsNullOrWhiteSpace(opts.Endpoint) ||
            string.IsNullOrWhiteSpace(opts.AccessKey) ||
            string.IsNullOrWhiteSpace(opts.SecretKey))
        {
            throw new InvalidOperationException("S3 Endpoint, AccessKey, and SecretKey must all be configured.");
        }

        services.AddSingleton<IAmazonS3>(new AmazonS3Client(
            new BasicAWSCredentials(opts.AccessKey, opts.SecretKey),
            new AmazonS3Config { ServiceURL = opts.Endpoint, ForcePathStyle = true }));

        return services;
    }

    private static IServiceCollection AddEfCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is missing.");

        services.AddDbContext<IngestionDbContext>(opts =>
            opts.UseNpgsql(connectionString, o => o.UseVector()));

        return services;
    }

    private static IServiceCollection AddEmbeddingGenerator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration.GetSection(OllamaOptions.SectionName).Get<OllamaOptions>()
            ?? throw new InvalidOperationException("Ollama options are missing.");

        services.AddHttpClient("OllamaEmbedding", client =>
        {
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.AddEmbeddingGenerator(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("OllamaEmbedding");
            return new OllamaApiClient(httpClient, opts.EmbeddingModel);
        });

        return services;
    }

    public static void ConfigureWolverine(this WolverineOptions opts, IConfiguration configuration)
    {
        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMQ options are missing.");

        opts.Discovery.IncludeAssembly(typeof(DocumentUploadedHandler).Assembly);
        opts.DefaultExecutionTimeout = TimeSpan.FromSeconds(300);

        opts.Policies.UseDurableLocalQueues();

        opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

        opts.ListenToRabbitQueue("document-uploaded")
            .Sequential();

        opts.ListenToRabbitQueue("document-deleted");

        opts.PublishMessage<DocumentIndexedEvent>()
            .ToRabbitQueue("document-indexed");

        opts.PublishMessage<DocumentIndexingFailedEvent>()
            .ToRabbitQueue("document-failed");
    }
}