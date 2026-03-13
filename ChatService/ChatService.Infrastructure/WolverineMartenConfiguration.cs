using BuildingBlocks.Core;
using ChatService.Domain.Message.Events;
using ChatService.Domain.Session.Events;
using ChatService.Infrastructure.EventStore;
using ChatService.Infrastructure.Projections;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Infrastructure;

public static class WolverineMartenConfiguration
{
    public static void ConfigureWolverineMarten(this IServiceCollection services, string connectionString)
    {
        // Configure Marten
        services.AddMarten(sp =>
        {
            var opts = new StoreOptions();

            opts.Connection(connectionString);
            opts.Events.AddEventType<SessionCreatedEvent>();
            opts.Events.AddEventType<MessageCreatedEvent>();
            opts.Events.AddEventType<SessionDeletedEvent>();
            opts.Projections.Add<ConversationProjection>(ProjectionLifecycle.Inline);
            opts.Projections.Add<MessageProjection>(ProjectionLifecycle.Inline);

            return opts;
        });

        // Ensure Marten document store is accessible via DI
        services.AddScoped<IDocumentSession>(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddScoped<IQuerySession>(sp => sp.GetRequiredService<IDocumentStore>().QuerySession());

        // Register Marten-backed event store repository adapter for aggregates
        services.AddScoped(typeof(IEventStoreRepository<>), typeof(MartenEventStoreRepository<>));

        // Register Marten-backed read-only event store for queries
        services.AddScoped<IReadOnlyEventStore, MartenReadOnlyEventStore>();
    }
}
