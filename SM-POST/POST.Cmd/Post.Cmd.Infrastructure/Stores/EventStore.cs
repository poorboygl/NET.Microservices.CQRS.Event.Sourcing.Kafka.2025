using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Exceptions;
using CQRS.Core.Infrastructure;
using Post.Cmd.Domain.Aggregates;

namespace Post.Cmd.Infrastructure.Stores;

public class EventStore(IEventStoreRepository eventStoreRepository) : IEventStore
{
    public async Task<List<BaseEvent>> GetEventsAsync(Guid aggregateId)
    {
        var eventSteam = await eventStoreRepository.FindByAggregateId(aggregateId);

        if (eventSteam == null || eventSteam.Any())
        {
            throw new AggregateNotFoundException("InCorrect post ID provided!");
        }

        // return eventSteam.OrderBy(x => x.Version).Select(x => x.EventData).ToList();
        return [.. eventSteam.OrderBy(x => x.Version).Select(x => x.EventData)];
    }

    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<BaseEvent> events, int expectedVersion)
    {
        var eventSteam = await eventStoreRepository.FindByAggregateId(aggregateId);

        if (expectedVersion != -1 && eventSteam[^1].Version != expectedVersion)
        {
            throw new ConcurrencyException();
        }

        var version = expectedVersion;

        foreach (var @event in events)
        {
            version++;
            @event.Version = version;
            var eventType = @event.GetType().Name;
            var eventModel = new EventModel
            {
                TimeStamp = DateTime.Now,
                AggregateIdentifier = aggregateId,
                AggregateType = nameof(PostAggregate),
                Version = version,
                EventType = eventType,
                EventData = @event
            };

            await eventStoreRepository.SaveAsync(eventModel);
        }
    }
}
