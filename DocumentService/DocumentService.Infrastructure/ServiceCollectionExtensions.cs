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
using Microsoft.Extensions.Options;
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

        var s3Config = new AmazonS3Config
        {
            ServiceURL = s3Options.Endpoint,
            ForcePathStyle = true
        };

        var credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);
        services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));

        return services;
    }

    public static void ConfigureWolverine(this WolverineOptions opts, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMQ options are missing.");

        opts.Discovery.IncludeAssembly(typeof(UploadDocumentCommand).Assembly);
        opts.Policies.AutoApplyTransactions();
        opts.Policies.UseDurableLocalQueues();

        opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

        opts.PublishMessage<DocumentUploadedEvent>()
            .ToRabbitQueue("document-uploaded");

        opts.PublishMessage<DocumentDeletedEvent>()
            .ToRabbitQueue("document-deleted");

        opts.ListenToRabbitQueue("document-indexed")
            .PreFetchCount(10);

        opts.ListenToRabbitQueue("document-failed")
            .PreFetchCount(10);
    }
}