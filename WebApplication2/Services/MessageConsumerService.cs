using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace WebApplication2.Services;

public class MessageConsumerService(
    IConnection rabbitConnection,
    ILogger<MessageConsumerService> logger) : BackgroundService
{
    private const string QueueName = "messages";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<QueueMessage>(eventArgs.Body.Span);
                logger.LogInformation(
                    "Kuyruktan mesaj alindi. Id: {Id}, Text: {Text}, CreatedAtUtc: {CreatedAtUtc}",
                    message?.Id, message?.Text, message?.CreatedAtUtc);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Mesaj islenirken hata olustu, kuyruga geri gonderiliyor");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("'{Queue}' kuyrugu dinleniyor", QueueName);
    }
}

public record QueueMessage(Guid Id, string Text, DateTime CreatedAtUtc);
