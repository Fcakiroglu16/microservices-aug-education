using System.Net;
using System.Text;
using System.Text.Json;
using One.API.Clients;
using RabbitMQ.Client;
using SharedLibrary;
using SharedLibrary.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IConnection>(_ =>
{
    var connectionFactory = new ConnectionFactory
    {
        Uri = new Uri("amqps://smahhmfk:U3QKCkbQrbXDwrE4ALgfaOua2XC8OVN8@leopard.lmq.cloudamqp.com/smahhmfk")
    };

    return connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// HttpClient'i DI Container'a ekliyoruz (typed client => TwoApiClient'in ctor'una inject edilir)
// Sira onemli: once eklenen disda kalir.
//
// DIKKAT: Asagidaki degerler DERS/DEMO icindir; Postman'den elle tiklayarak her policy'nin
// tetiklenebilmesi icin bilerek dusuk tutulmustur. Production degerleri icin her policy
// dosyasindaki Default* sabitlerine bakin (orn. minimumThroughput: 10, breakDuration: 30 sn).
//
// AddFallbackPolicy         => tum policy'ler tukenirse bos urun listesi doner (en disda)
// AddConcurrencyLimitPolicy => ayni anda en fazla 3 istek (Postman Runner ile 4+ paralel istek atinca reddedilir)
// AddRetryPolicy            => 3 kez retry: 1sn, 2sn, 4sn (varsayilan 2/4/8sn Postman'de cok uzun bekletiyor)
// AddHedgingPolicy          => 500 ms icinde cevap gelmezse paralel deneme acar (orijinal + 2 deneme)
//                              (Two.API /api/products olculen gecikme: p50 0,3 ms - p99 0,5 ms => 500 ms ~1000x p99,
//                               saglikli trafikte hic tetiklenmez)
// AddCircuitBreakerPolicy   => 30 sn'lik periyotta 3 istekten %50'si hatali => devre 15 sn acik kalir
//                              (varsayilan minimumThroughput: 10 + samplingDuration: 10 sn => en az 1 rps gerekir,
//                               elle tiklayarak o trafige ulasilamadigi icin devre hic acilmazdi)
// AddTimeoutPolicy          => her deneme icin 3 sn timeout (en icte, HttpClient.Timeout'u da yonetir)
builder.Services.AddHttpClient<TwoApiClient>(client =>
        client.BaseAddress = new Uri(builder.Configuration["TwoApi:BaseAddress"]!))
    .AddFallbackPolicy(() => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(Array.Empty<ProductDto>())
    })
    .AddConcurrencyLimitPolicy(permitLimit: 3)
    .AddRetryPolicy(maxRetryAttempts: 3, delay: TimeSpan.FromSeconds(1))
    .AddHedgingPolicy(delay: TimeSpan.FromMilliseconds(500))
    .AddCircuitBreakerPolicy(
        failureRatio: 0.5,
        samplingDuration: TimeSpan.FromSeconds(30),
        breakDuration: TimeSpan.FromSeconds(15),
        minimumThroughput: 3)
    .AddTimeoutPolicy(TimeSpan.FromSeconds(3));

var app = builder.Build();

app.MapPost("/api/users",
    async (CreateUserRequest request, IConnection connection, CancellationToken cancellationToken) =>
    {
        
        //Outbox Pattern
        // 1. Aşama OutboxTable(Id,EventType,EventAsJson,IsPublished) - 1,UserCreatedEvent,{userId:1,...},false
        
        // begin transaction
        // user save
        // outboxRow save
        
        // end transaction
        
        
        
        
        
        var userId = 1000;
        var userCreatedEvent = new UserCreatedEvent(userId, request.UserName, request.Email);


        var userCreatedEventAsJson = JsonSerializer.Serialize(userCreatedEvent);

        var userCreatedEventAsJsonBytes = Encoding.UTF8.GetBytes(userCreatedEventAsJson);

        const string exchangeName = "one.api-user.created-exchange";
        // await PublishWithoutAckAsync(connection, exchangeName, userCreatedEventAsJsonBytes);
        await PublishWithAckAsync(connection, exchangeName, userCreatedEventAsJsonBytes, app.Logger, cancellationToken);


        return Results.Ok();
    });


app.MapGet("/api/products-from-two",
    async (TwoApiClient twoApiClient, CancellationToken cancellationToken) =>
    {
        var products = await twoApiClient.GetProductsAsync(cancellationToken);

        return Results.Ok(products);
    });


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();


app.Run();

static async Task PublishWithoutAckAsync(IConnection connection, string exchangeName, byte[] eventBody)
{
    var channel = await connection.CreateChannelAsync();
    await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, true);
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
    await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Fanout, true);
    while (attempt < maxPublishAttempts)
    {
        attempt++;
        try
        {
            
            
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            
            cancellationTokenSource.CancelAfter(3000);
            
            await channel.BasicPublishAsync(exchangeName, string.Empty, false, eventBody,cancellationTokenSource.Token);
            
            
            
            
            break;
        }
        catch (Exception ex)
        {
            if (attempt >= maxPublishAttempts) throw;

            logger.LogWarning(ex,
                "Publishing user created event failed on attempt {Attempt}/{MaxAttempts}. Retrying...",
                attempt, maxPublishAttempts);
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }
}

public record CreateUserRequest(string UserName, string Email);