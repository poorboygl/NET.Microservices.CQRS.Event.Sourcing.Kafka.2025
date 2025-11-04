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

        Console.WriteLine($"🟢 Kafka consumer started. Subscribed to topic: {topic}");

        while (true)
        {
            try
            {
                //Dev-Test
                var consumerResult = consumer.Consume(TimeSpan.FromSeconds(2));

                //Product
                // var consumerResult = consumer.Consume(TimeSpan.FromMilliseconds(500);

                if (consumerResult == null) continue;
                if (consumerResult.Message == null) continue;

                var options = new JsonSerializerOptions { Converters = { new EventJsonConverter() } };
                var @event = JsonSerializer.Deserialize<BaseEvent>(consumerResult.Message.Value, options);

                // var handlerMethod = _eventHandler.GetType().GetMethod("On", new[] { @event.GetType() });
                var handlerMethod = _eventHandler.GetType().GetMethod("On", [@event.GetType()]);
                if (handlerMethod == null)
                {
                    Console.WriteLine($"⚠️ No handler found for {@event.GetType().Name}");
                    continue;
                }

                handlerMethod.Invoke(_eventHandler, new object[] { @event });
                consumer.Commit(consumerResult);

                Console.WriteLine($"✅ Consumed event {@event.GetType().Name} from offset {consumerResult.Offset}");
            }
            catch (ConsumeException ex)
            {
                // No topic- or Kafka is not ready
                if (ex.Error.Reason.Contains("Unknown topic or partition"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️ Kafka topic '{topic}' does not exist yet. Waiting 5 seconds before retrying...");
                    Console.ResetColor();
                    Thread.Sleep(5000);
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Kafka consume error: {ex.Error.Reason}");
                Console.ResetColor();
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Unhandled consumer error: {ex.Message}");
                Console.ResetColor();
                Thread.Sleep(2000);
            }
        }
    }
}