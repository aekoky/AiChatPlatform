using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Core;
using ChatService.Application.Features.StartChat;
using ChatService.Application.Sagas;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session.Events;
using ChatService.Infrastructure.EventStore;
using ChatService.Infrastructure.Projections;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
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
            opts.Events.AddEventType<SessionDeletedEvent>();

            // Inline projections (synchronous, no daemon needed)
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
        opts.Discovery.IncludeAssembly(typeof(StartChatCommand).Assembly);
        opts.Discovery.IncludeAssembly(typeof(ConversationSaga).Assembly);
        opts.Policies.AutoApplyTransactions();
        opts.Policies.UseDurableLocalQueues();

        opts.UseRabbitMq(new Uri(configuration["RabbitMQ:Uri"]!));

        // ChatService → AiService: send LLM request
        opts.PublishMessage<LlmResponseRequestedEvent>()
            .ToRabbitQueue("llm-requests");

        // AiService → ChatService: receive completed response, route to saga
        opts.ListenToRabbitQueue("llm-completed")
            .PreFetchCount(10);

        opts.ListenToRabbitQueue("llm-gave-up.chatservice")
            .PreFetchCount(10);
    }
}