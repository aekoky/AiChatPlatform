using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using NotificationService.Api;
using NotificationService.Api.Options;
using NotificationService.Api.Services;
using NotificationService.Application.Handlers;
using NotificationService.Application.Services;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatHub>();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

var rabbitOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
    ?? throw new InvalidOperationException("RabbitMq options are missing.");

var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>()
    ?? throw new InvalidOperationException("Keycloak options are missing.");

builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakOptions.Authority;
        options.Audience = keycloakOptions.Audience;
        options.RequireHttpsMetadata = false; // set to true in production
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs/chat"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(LlmTokensGeneratedHandler).Assembly);
    opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

    opts.ListenToRabbitQueue("llm-tokens").Sequential();
    opts.ListenToRabbitQueue("llm-sources");
    opts.ListenToRabbitQueue("llm-retrying");
    opts.ListenToRabbitQueue("llm-completed.notificationservice");
    opts.ListenToRabbitQueue("llm-gave-up.notificationservice");
    opts.ListenToRabbitQueue("session-notifications");
});


var app = builder.Build();

// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");

app.Run();
