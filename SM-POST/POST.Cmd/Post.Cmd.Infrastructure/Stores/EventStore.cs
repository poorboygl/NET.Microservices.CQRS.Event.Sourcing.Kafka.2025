using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Exceptions;
using CQRS.Core.Infrastructure;
using CQRS.Core.Producers;
using Post.Cmd.Domain.Aggregates;

namespace Post.Cmd.Infrastructure.Stores;

public class EventStore(IEventStoreRepository eventStoreRepository, IEventProducer eventProducer) : IEventStore
{
    public async Task<List<Guid>> GetAggregateIdsAsync()
    {
        var eventSteam = await eventStoreRepository.FindAllAsync();

        if (eventSteam == null || eventSteam.Count == 0)
        {
            throw new ArgumentNullException(nameof(eventSteam), "Could not retrieve event stream from the event store!");
        }

        //return eventSteam.Select(x => x.AggregateIdentifier).Distinct().ToList();
        return [.. eventSteam.Select(x => x.AggregateIdentifier).Distinct()];
    }

    public async Task<List<BaseEvent>> GetEventsAsync(Guid aggregateId)
    {
        var eventSteam = await eventStoreRepository.FindByAggregateId(aggregateId);

        if (eventSteam == null || eventSteam.Count == 0)
        {
            throw new AggregateNotFoundException("Incorrect post ID provided!");
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

            var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
            await eventProducer.ProduceAsync(topic!, @event);
        }
    }
}
