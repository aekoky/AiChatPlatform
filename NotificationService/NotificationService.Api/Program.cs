using Microsoft.AspNetCore.Authentication.JwtBearer;
using NotificationService.Api;
using NotificationService.Api.Services;
using NotificationService.Application.Commands;
using NotificationService.Application.Services;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatHub>();
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false; // set to true in production
    });

builder.Services.AddAuthorization();

// 3. Wolverine with RabbitMQ
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(LlmTokenGeneratedHandler).Assembly);
    opts.UseRabbitMq(new Uri(builder.Configuration["RabbitMQ:Uri"]!));

    opts.ListenToRabbitQueue("llm-tokens");
    opts.ListenToRabbitQueue("llm-completed.notificationservice");
    opts.ListenToRabbitQueue("llm-gave-up.notificationservice");
});


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");

app.Run();
