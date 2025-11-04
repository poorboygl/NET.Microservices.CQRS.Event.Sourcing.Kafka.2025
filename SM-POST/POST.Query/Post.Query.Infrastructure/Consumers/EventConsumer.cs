using System.Text.Json;
using Confluent.Kafka;
using CQRS.Core.Consumers;
using CQRS.Core.Events;
using Microsoft.Extensions.Options;
using Post.Query.Infrastructure.Converters;
using Post.Query.Infrastructure.Handlers;

namespace Post.Query.Infrastructure.Consumers;

public class EventConsumer(IOptions<ConsumerConfig> config, IEventHandler eventHandler) : IEventConsumer
{
    private readonly ConsumerConfig _config = config.Value;
    private readonly IEventHandler _eventHandler = eventHandler;

    public void Consumer(string topic)
    {
        using var consumer = new ConsumerBuilder<string, string>(_config)
                                 .SetKeyDeserializer(Deserializers.Utf8)
                                 .SetValueDeserializer(Deserializers.Utf8)
                                 .Build();

        consumer.Subscribe(topic);

        while (true)
        {
            var consumerResult = consumer.Consume();

            if (consumerResult?.Message == null) continue;

            var options = new JsonSerializerOptions { Converters = { new EventJsonConverter() } };
            var @event = JsonSerializer.Deserialize<BaseEvent>(consumerResult.Message.Value, options);
            var handlerMethod = _eventHandler.GetType().GetMethod("On", new Type[] { @event.GetType() });

            if (handlerMethod == null)
            {
                throw new ArgumentException(nameof(handlerMethod), "Could not find event handler method!");
            }

            handlerMethod.Invoke(_eventHandler, new object[] { @event });
            consumer.Commit(consumerResult);

        }
    }
}
