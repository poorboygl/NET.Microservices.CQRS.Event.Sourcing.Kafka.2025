namespace CQRS.Core.Consumers;

public interface IEventConsumer
{
    void Consumer(string topic);
}
