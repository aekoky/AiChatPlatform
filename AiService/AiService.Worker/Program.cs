using AiService.Application.Commands;
using AiService.Infrastructure.Extensions;
using AiService.Infrastructure.Options;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Core.Options;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddLlmClient();

builder.UseWolverine(opts =>
{
    var serviceProvider = builder.Services.BuildServiceProvider();
    var rabbitOptions = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

    opts.Discovery.IncludeAssembly(typeof(GenerateAiResponseHandler).Assembly);
    opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

    // Receive LLM requests from ChatService — sequential to preserve order per session
    opts.ListenToRabbitQueue("llm-requests")
        .Sequential();

    opts.PublishMessage<LlmTokenGeneratedEvent>()
        .ToRabbitQueue("llm-tokens");

    // Broadcast llm-completed to fanout exchange → chatservice + notificationservice queues
    opts.PublishMessage<LlmResponseCompletedEvent>()
        .ToRabbitExchange("llm-completed");

    // Broadcast gave-up to fanout exchange → chatservice + notificationservice queues
    opts.PublishMessage<LlmResponseGaveUpEvent>()
        .ToRabbitExchange("llm-gave-up");
});

var app = builder.Build();

app.Run();
