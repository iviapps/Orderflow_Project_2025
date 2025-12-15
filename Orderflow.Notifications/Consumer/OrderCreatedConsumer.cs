using MassTransit;
using Orderflow.Shared.Events;

namespace Orderflow.Notifications.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var @event = context.Message;

        logger.LogInformation(
            "Processing OrderCreatedEvent: EventId={EventId}, OrderId={OrderId}, UserId={UserId}, Items={ItemCount}",
            @event.EventId, @event.OrderId, @event.UserId, @event.Items.Count());

        // Future: Send order confirmation email, trigger inventory updates, etc.

        return Task.CompletedTask;
    }
}
