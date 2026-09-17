namespace ObservabilityOne.API;

// Fanout exchange üzerinden ObservabilityTwo'ya yayınlanan event.
// TraceId, consumer tarafında log'ları producer trace'i ile ilişkilendirmek için taşınır.
public record UserCreatedEvent(Guid UserId, string Name, string Email, DateTimeOffset CreatedAtUtc, string TraceId);

public static class RabbitMqTopology
{
    public const string UserCreatedFanoutExchange = "user-created-fanout";
}
