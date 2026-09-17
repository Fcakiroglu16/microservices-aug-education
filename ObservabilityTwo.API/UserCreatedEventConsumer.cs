using System.Diagnostics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ObservabilityTwo.API;

// user-created-fanout exchange'ine bağlı kuyruktan UserCreatedEvent tüketir,
// producer'ın traceparent header'ını okuyarak kendi Activity'sini producer trace'ine bağlar.
public class UserCreatedEventConsumer(IConnection connection, ILogger<UserCreatedEventConsumer> logger) : BackgroundService
{
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            RabbitMqTopology.UserCreatedFanoutExchange,
            ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            RabbitMqTopology.UserCreatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            RabbitMqTopology.UserCreatedQueue,
            RabbitMqTopology.UserCreatedFanoutExchange,
            routingKey: string.Empty,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnUserCreatedEventReceivedAsync;

        await _channel.BasicConsumeAsync(
            RabbitMqTopology.UserCreatedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "UserCreatedEvent consumer started. Exchange: {Exchange} Queue: {Queue}",
            RabbitMqTopology.UserCreatedFanoutExchange, RabbitMqTopology.UserCreatedQueue);

        // BackgroundService'i host durdurulana kadar canlı tut; iş asenkron event handler'da yapılıyor.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Beklenen durum: servis durdurulurken tetiklenir.
        }
    }

    private async Task OnUserCreatedEventReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var parentContext = ExtractParentTraceContext(ea.BasicProperties);

        using var activity = ActivitySourceProvider._activitySource?.StartActivity(
            "UserCreatedEvent consume", ActivityKind.Consumer, parentContext);

        try
        {
            var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(ea.Body.Span);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", ea.Exchange);
            activity?.SetTag("messaging.destination_kind", "fanout");
            activity?.SetTag("user.id", userCreatedEvent?.UserId);

            var traceId = activity?.TraceId.ToString() ?? userCreatedEvent?.TraceId ?? "unknown";

            logger.LogInformation(
                "UserCreatedEvent consumed. TraceId: {TraceId} UserId: {UserId} Name: {Name} Email: {Email}",
                traceId, userCreatedEvent?.UserId, userCreatedEvent?.Name, userCreatedEvent?.Email);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UserCreatedEvent processed with error, message will be requeued.");
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static ActivityContext ExtractParentTraceContext(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null ||
            !properties.Headers.TryGetValue("traceparent", out var traceparentValue) ||
            traceparentValue is not byte[] traceparentBytes)
        {
            return default;
        }

        var traceparent = Encoding.UTF8.GetString(traceparentBytes);

        string? tracestate = null;
        if (properties.Headers.TryGetValue("tracestate", out var tracestateValue) && tracestateValue is byte[] tracestateBytes)
        {
            tracestate = Encoding.UTF8.GetString(tracestateBytes);
        }

        return ActivityContext.TryParse(traceparent, tracestate, out var parentContext)
            ? parentContext
            : default;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
