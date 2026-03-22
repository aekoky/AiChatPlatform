using BuildingBlocks.Contracts.DocumentEvents;
using DocumentIngestion.Application.Handlers;
using DocumentIngestion.Infrastructure;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddIngestionInfrastructure(builder.Configuration);

builder.UseWolverine(opts =>
{
    var rabbitUri = builder.Configuration.GetSection("RabbitMQ:Uri").Value
        ?? "amqp://guest:guest@localhost:5672";

    var ollamaTimeoutString = builder.Configuration.GetSection("Ollama:TimeoutSeconds").Value;
    int.TryParse(ollamaTimeoutString, out var ollamaTimeout);

    opts.Discovery.IncludeAssembly(typeof(DocumentUploadedHandler).Assembly);
    opts.DefaultExecutionTimeout = TimeSpan.FromSeconds(ollamaTimeout > 0 ? ollamaTimeout : 300);

    opts.Policies.UseDurableLocalQueues();

    opts.UseRabbitMq(new Uri(rabbitUri));

    opts.ListenToRabbitQueue("document-uploaded")
        .Sequential();

    opts.ListenToRabbitQueue("document-deleted");

    opts.PublishMessage<DocumentIndexedEvent>()
        .ToRabbitQueue("document-indexed");

    opts.PublishMessage<DocumentIndexingFailedEvent>()
        .ToRabbitQueue("document-failed");
});

var app = builder.Build();
app.Run();