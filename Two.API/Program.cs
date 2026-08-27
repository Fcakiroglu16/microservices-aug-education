using RabbitMQ.Client;
using Two.API.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHostedService<UserCreatedEventConsumer>();
builder.Services.AddSingleton(_ =>
{
    var connectionFactory = new ConnectionFactory
    {
        Uri = new Uri("amqps://smahhmfk:U3QKCkbQrbXDwrE4ALgfaOua2XC8OVN8@leopard.lmq.cloudamqp.com/smahhmfk")
    };

    return connectionFactory.CreateConnectionAsync().Result;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();


app.Run();