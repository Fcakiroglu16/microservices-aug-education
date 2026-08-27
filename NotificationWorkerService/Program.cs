using NotificationWorkerService.Consumers;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<UserCreatedEventConsumer>();
builder.Services.AddSingleton(_ =>
{
    var connectionFactory = new ConnectionFactory
    {
        Uri = new Uri("amqps://smahhmfk:U3QKCkbQrbXDwrE4ALgfaOua2XC8OVN8@leopard.lmq.cloudamqp.com/smahhmfk")
    };

    return connectionFactory.CreateConnectionAsync().Result;
});
var host = builder.Build();
host.Run();