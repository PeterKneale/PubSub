namespace Messages;

public class OrderSubmittedEvent
{
    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }
}