using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ObservabilityOne.API;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("rabbitmq");
var applicationName = builder.Environment.ApplicationName;

ActivitySourceProvider._activitySource = new ActivitySource(applicationName);
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// HttpClient resolved via Aspire service discovery to reach the ObservabilityTwo API.
builder.Services.AddHttpClient("observabilitytwo", client =>
{
    client.BaseAddress = new Uri("https+http://observabilitytwo");
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/products", async (IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("observabilitytwo");
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        return Results.Ok(products);
    })
    .WithName("GetProductsFromObservabilityTwo");

app.MapPost("/api/users", async (CreateUserRequest request, IConnection connection, ILogger<Program> logger) =>
    {
        // Producer tarafı: RabbitMQ'ya yayınlanan mesajı temsil eden bir Activity başlatılıyor.
        // Bu Activity, ASP.NET Core'un HTTP isteği için oluşturduğu Activity'nin (Activity.Current) çocuğu olur.
        using var activity = ActivitySourceProvider._activitySource?.StartActivity(
            "UserCreatedEvent publish", ActivityKind.Producer);

        var traceId = activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        var userCreatedEvent = new UserCreatedEvent(
            Guid.NewGuid(),
            request.Name,
            request.Email,
            DateTimeOffset.UtcNow,
            traceId);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", RabbitMqTopology.UserCreatedFanoutExchange);
        activity?.SetTag("messaging.destination_kind", "fanout");
        activity?.SetTag("user.id", userCreatedEvent.UserId);

        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(RabbitMqTopology.UserCreatedFanoutExchange, ExchangeType.Fanout, durable: true);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Headers = new Dictionary<string, object?>()
        };

        // W3C trace context'i mesaj header'ına taşıyoruz ki consumer kendi trace'ini bununla ilişkilendirebilsin.
        if (activity is not null)
        {
            properties.Headers["traceparent"] = Encoding.UTF8.GetBytes(activity.Id!);
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                properties.Headers["tracestate"] = Encoding.UTF8.GetBytes(activity.TraceStateString);
            }
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(userCreatedEvent);

        await channel.BasicPublishAsync(
            RabbitMqTopology.UserCreatedFanoutExchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: properties,
            body: body);

        logger.LogInformation(
            "UserCreatedEvent published. TraceId: {TraceId} UserId: {UserId} Exchange: {Exchange}",
            traceId, userCreatedEvent.UserId, RabbitMqTopology.UserCreatedFanoutExchange);

        return Results.Accepted(value: userCreatedEvent);
    })
    .WithName("PublishUserCreatedEvent");

app.Run();

record ProductDto(int Id, string Name, decimal Price);

record CreateUserRequest(string Name, string Email);

