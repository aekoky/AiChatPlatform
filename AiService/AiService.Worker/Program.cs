using AiService.Application.Commands;
using AiService.Infrastructure.Extensions;
using AiService.Infrastructure.Options;
using BuildingBlocks.Contracts.Events;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddOpenAILlmClient();

builder.UseWolverine(opts =>
{
    var serviceProvider = builder.Services.BuildServiceProvider();
    var rabbitOptions = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

    opts.Discovery.IncludeAssembly(typeof(GenerateAiResponseHandler).Assembly);
    opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

    opts.ListenToRabbitQueue("llm-requests")
        .Sequential();

    opts.PublishMessage<LlmTokenGeneratedEvent>()
        .ToRabbitQueue("llm-tokens");

    opts.PublishMessage<LlmResponseCompletedEvent>()
        .ToRabbitExchange("llm-completed");

    opts.PublishMessage<LlmResponseRetryingEvent>()
        .ToRabbitQueue("llm-retrying");

    opts.PublishMessage<LlmResponseGaveUpEvent>()
        .ToRabbitExchange("llm-gave-up");
});

var app = builder.Build();

app.Run();
