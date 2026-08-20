using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SharedLibrary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapPost("/api/users", async (CreateUserRequest request,CancellationToken cancellationToken) =>
{
    // user created for db
    var userId = 1000;
    var userCreatedEvent = new UserCreatedEvent(userId, request.UserName, request.Email);

    var userCreatedEventAsJson = JsonSerializer.Serialize(userCreatedEvent);
    
    var userCreatedEventAsJsonBytes = Encoding.UTF8.GetBytes(userCreatedEventAsJson);

    var connectionFactory= new ConnectionFactory() { Uri = new Uri("amqp://localhost") };

    await using var connection =await connectionFactory.CreateConnectionAsync();

    const string exchangeName = "one.api-user.created-exchange";
    await PublishWithoutAckAsync(connection, exchangeName, userCreatedEventAsJsonBytes);
    await PublishWithAckAsync(connection, exchangeName, userCreatedEventAsJsonBytes, app.Logger, cancellationToken);
    
    
    return Results.Ok();
});



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();



app.Run();

static async Task PublishWithoutAckAsync(IConnection connection, string exchangeName, byte[] eventBody)
{
    var channel = await connection.CreateChannelAsync();
    await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout, durable: true);
    await channel.BasicPublishAsync(exchangeName, string.Empty, true, eventBody);
}

static async Task PublishWithAckAsync(
    IConnection connection,
    string exchangeName,
    byte[] eventBody,
    ILogger logger,
    CancellationToken cancellationToken)
{
    var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true));
    const int maxPublishAttempts = 3;
    var attempt = 0;

    while (attempt < maxPublishAttempts)
    {
        attempt++;
        try
        {
            await channel.BasicPublishAsync(exchangeName, string.Empty, true, eventBody);
            break;
        }
        catch (Exception ex)
        {
            if (attempt >= maxPublishAttempts)
            {
                throw;
            }

            logger.LogWarning(ex,
                "Publishing user created event failed on attempt {Attempt}/{MaxAttempts}. Retrying...",
                attempt, maxPublishAttempts);
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }
}

public record CreateUserRequest(string UserName, string Email);
