using BuildingBlocks.Contracts.LlmEvents;
using BuildingBlocks.Contracts.SessionEvents;
using BuildingBlocks.Core;
using ChatService.Application.Features.StartChat;
using ChatService.Application.Sagas;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session.Events;
using ChatService.Infrastructure.EventStore;
using ChatService.Infrastructure.Options;
using ChatService.Infrastructure.Projections;
using JasperFx.Core;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

namespace ChatService.Infrastructure;

public static class WolverineMartenConfiguration
{
    public static void ConfigureWolverineMarten(this IServiceCollection services, string connectionString)
    {
        services.AddMarten(sp =>
        {
            var opts = new StoreOptions();
            opts.Connection(connectionString);
            // Marten event types
            opts.Events.AddEventType<SessionCreatedEvent>();
            opts.Events.AddEventType<MessageCreatedEvent>();
            opts.Events.AddEventType<SessionUpdatedEvent>();
            opts.Events.AddEventType<SessionDeletedEvent>();
            opts.Events.AddEventType<SessionSummaryUpdatedEvent>();
            opts.Events.AddEventType<SessionTitleUpdatedEvent>();

            // Inline projections (to prevent stale reads right after write)
            opts.Projections.Add<ConversationProjection>(ProjectionLifecycle.Inline);
            opts.Projections.Add<MessageProjection>(ProjectionLifecycle.Inline);

            return opts;
        })
        // HotCold: required for PublishEventsToWolverine subscriptions
        .AddAsyncDaemon(DaemonMode.HotCold)
        // Wolverine/Marten integration: saga persistence + outbox + event forwarding
        .IntegrateWithWolverine();

        // Marten sessions for DI
        services.AddScoped<IDocumentSession>(sp =>
            sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped<IQuerySession>(sp =>
            sp.GetRequiredService<IDocumentStore>().QuerySession());

        // Event store abstractions
        services.AddScoped(typeof(IEventStoreRepository<>), typeof(MartenEventStoreRepository<>));
        services.AddScoped<IReadOnlyEventStore, MartenReadOnlyEventStore>();
    }

    public static void ConfigureWolverine(this WolverineOptions opts, IConfiguration configuration)
    {
        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMQ options are missing.");

        opts.Discovery.IncludeAssembly(typeof(StartChatCommand).Assembly);
        opts.Discovery.IncludeAssembly(typeof(ConversationSaga).Assembly);

        // Handle pessimistic saga concurrency failures by jittering retries
        opts.Policies.OnException<TimeoutException>()
            .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
        opts.Policies.AutoApplyTransactions();

        opts.UseRabbitMq(new Uri(rabbitOptions.Uri));

        // ChatService → AiService: send LLM request
        opts.PublishMessage<LlmResponseRequestedEvent>()
            .ToRabbitQueue("llm-requests")
            .UseDurableOutbox();

        opts.PublishMessage<SessionSummarizeRequestedEvent>()
            .ToRabbitQueue("llm-summarization")
            .UseDurableOutbox();

        opts.PublishMessage<SessionTitleUpdatedNotificationEvent>()
            .ToRabbitExchange("session-notifications")
            .UseDurableOutbox();

        opts.PublishMessage<SessionSummaryUpdatedNotificationEvent>()
            .ToRabbitExchange("session-notifications")
            .UseDurableOutbox();

        // AiService → ChatService: receive completed response, route to saga
        opts.ListenToRabbitQueue("llm-completed.chatservice")
            .UseDurableInbox();

        opts.ListenToRabbitQueue("llm-gave-up.chatservice")
            .UseDurableInbox();

        opts.ListenToRabbitQueue("summary-tokens")
            .UseDurableInbox();
    }
}