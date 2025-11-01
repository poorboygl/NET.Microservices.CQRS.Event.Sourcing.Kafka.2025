using CQRS.Core.Domain;
using CQRS.Core.Handlers;
using Post.Cmd.Domain.Aggregates;
using Post.Cmd.Infrastructure.Stores;

namespace Post.Cmd.Infrastructure.Handlers;

public class EventSourcingHandler(EventStore eventStore) : IEventSourcingHandler<PostAggregate>
{
    public async Task<PostAggregate> GetByIdAsync(Guid aggregateId)
    {
        var aggregate = new PostAggregate();
        var events = await eventStore.GetEventsAsync(aggregateId);

        if (events == null || events.Any()) return aggregate;

        aggregate.ReplayEvents(events);
        aggregate.Version = events.Select(x => x.Version).Max();
        return aggregate;
    }

    public async Task SaveAsync(AggregateRoot aggregate)
    {
        await eventStore.SaveEventsAsync(aggregate.Id, aggregate.GetUncommittedChanges(), aggregate.Version);
        aggregate.MarkChangesAsCommitted();
    }
}
