using MassTransit;
using Orderflow.Shared.Events;

namespace Orderflow.Notifications.Consumers;

public class OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var @event = context.Message;

        logger.LogInformation(
            "Processing OrderCancelledEvent: EventId={EventId}, OrderId={OrderId}, UserId={UserId}, Items={ItemCount}",
            @event.EventId, @event.OrderId, @event.UserId, @event.Items.Count());

        // Future: Send cancellation email, trigger refund process, etc.

        return Task.CompletedTask;
    }
}
