namespace ObservabilityTwo.API;

// ObservabilityOne'dan fanout exchange üzerinden gelen event ile aynı sözleşme.
public record UserCreatedEvent(Guid UserId, string Name, string Email, DateTimeOffset CreatedAtUtc, string TraceId);

public static class RabbitMqTopology
{
    public const string UserCreatedFanoutExchange = "user-created-fanout";
    public const string UserCreatedQueue = "observabilitytwo.user-created";
}
