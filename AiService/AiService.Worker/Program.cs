using AiService.Infrastructure;
using AiService.Infrastructure.Options;
using Wolverine;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

builder.Services.AddOllamaLlmClient();
builder.Services.AddRagRetrieval(builder.Configuration);

builder.UseWolverine(opts =>
{
    opts.ConfigureWolverine(builder.Services);
});

var app = builder.Build();

app.Run();
