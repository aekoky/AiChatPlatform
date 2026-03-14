using AiService.Application.Commands;
using AiService.Infrastructure.Extensions;
using BuildingBlocks.Contracts.Events;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLlmClient(builder.Configuration);

builder.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(GenerateAiResponseHandler).Assembly);
    opts.UseRabbitMq(new Uri(builder.Configuration["RabbitMQ:Uri"]!));

    // Receive LLM requests from ChatService — sequential to preserve order per session
    opts.ListenToRabbitQueue("llm-requests")
        .Sequential();

    // Stream tokens back to ChatService
    opts.PublishMessage<LlmTokenGeneratedEvent>()
        .ToRabbitQueue("llm-tokens");

    // Publish completed response to ChatService
    opts.PublishMessage<LlmResponseCompletedEvent>()
        .ToRabbitQueue("llm-completed");

    // Broadcast gave-up to fanout exchange → chatservice + notificationservice queues
    opts.PublishMessage<LlmResponseGaveUpEvent>()
        .ToRabbitExchange("llm-gave-up");
});

var app = builder.Build();

app.Run();
