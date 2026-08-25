using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Two.API.Consumers;

public class UserCreatedEventConsumer(IConnection connection,ILogger<UserCreatedEventConsumer> logger):BackgroundService
{
    private IChannel? _channel = null;
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        
        await _channel?.CloseAsync(cancellationToken: cancellationToken)!;
        await _channel.DisposeAsync();
        await connection.DisposeAsync();
        
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ => Consumer => Push
        // Consumer => Kafka/Redis => pull
        var queueName="two.api-user.created-queue";
       await _channel!.QueueDeclareAsync(queueName, true, false, false, null, cancellationToken: stoppingToken);
        
       await _channel.QueueBindAsync(queueName, "one.api-user.created-exchange", string.Empty, null, cancellationToken: stoppingToken);
        
        
        
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        
        
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
            
                var userCreatedEvent = System.Text.Json.JsonSerializer.Deserialize<SharedLibrary.UserCreatedEvent>(message);


              

                if (userCreatedEvent is not null)
                {
                    Console.WriteLine($"(Two.API)UserCreatedEvent received: {userCreatedEvent.UserId},{userCreatedEvent.UserName}, {userCreatedEvent.Email}");
                }


                await _channel!.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            }
            catch (Exception e)
            {
                
                logger.LogError(e, "Error processing message");
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, true, stoppingToken);
              
            }
       
        };
        
        
         await _channel!.BasicConsumeAsync(queueName, false, consumer, cancellationToken: stoppingToken);
        
        
    }
}