using Amazon.Runtime;
using Amazon.S3;
using BuildingBlocks.Contracts.DocumentEvents;
using DocumentService.Application.Features.UploadDocument;
using DocumentService.Application.Services;
using DocumentService.Infrastructure.Options;
using DocumentService.Infrastructure.Persistence;
using DocumentService.Infrastructure.Repositories;
using DocumentService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.RabbitMQ;

namespace DocumentService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEfCore(configuration);
        services.AddS3Storage(configuration);
        services.AddHttpClient("UrlDocumentsClient");
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IStorageService, S3StorageService>();

        return services;
    }

    private static IServiceCollection AddEfCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string is missing.");

        services.AddDbContext<DocumentDbContext>(opts =>
            opts.UseNpgsql(connectionString));

        return services;
    }

    private static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        var s3Options = configuration.GetSection(S3Options.SectionName).Get<S3Options>()
            ?? throw new InvalidOperationException("S3 options are missing.");

        if (string.IsNullOrWhiteSpace(s3Options.Endpoint) ||
            string.IsNullOrWhiteSpace(s3Options.AccessKey) ||
            string.IsNullOrWhiteSpace(s3Options.SecretKey))
        {
            throw new InvalidOperationException("S3 Endpoint, AccessKey, and SecretKey must all be configured.");
        }

        var s3Config = new AmazonS3Config
        {
            ServiceURL = s3Options.Endpoint,
            ForcePathStyle = true
        };

        var credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));

        return services;
    }

    public static void ConfigureWolverine(this WolverineOptions opts, IConfiguration configuration)
    {
        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMQ options are missing.");

        opts.Discovery.IncludeAssembly(typeof(UploadDocumentCommand).Assembly);
        opts.Policies.AutoApplyTransactions();

        opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

        opts.PublishMessage<DocumentUploadedEvent>()
            .ToRabbitQueue("document-uploaded")
            .UseDurableOutbox();

        opts.PublishMessage<DocumentDeletedEvent>()
            .ToRabbitQueue("document-deleted")
            .UseDurableOutbox();

        opts.ListenToRabbitQueue("document-indexed")
            .UseDurableInbox();

        opts.ListenToRabbitQueue("document-failed")
            .UseDurableInbox();
    }
}