using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Scalar.AspNetCore;
using StackExchange.Redis;
using WebApplication1.Data;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Aspire integrations: AppHost'taki "redis" ve "rabbitmq" kaynaklarina baglanir
builder.AddRedisClient("redis");
builder.AddRabbitMQClient("rabbitmq");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("WebApplication2", client =>
{
    // Aspire service discovery: AppHost'taki "webapplication2" kaynagina cozumlenir
    client.BaseAddress = new Uri("https+http://webapplication2");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.

app.MapOpenApi();
app.MapScalarApiReference();
//app.UseHttpsRedirection();




app.MapGet("/api/products", async (IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("WebApplication2");
        var products = await client.GetFromJsonAsync<List<Product>>("/api/products");
        return Results.Ok(products);
    })
    .WithName("GetProductsFromWebApplication2");

app.MapGet("/api/products/{id:int}", async (int id, IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("WebApplication2");
        var response = await client.GetAsync($"/api/products/{id}");

        if (!response.IsSuccessStatusCode)
            return Results.StatusCode((int)response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<Product>();
        return Results.Ok(product);
    })
    .WithName("GetProductByIdFromWebApplication2");

app.MapGet("/api/remote-users", async (IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("WebApplication2");
        var users = await client.GetFromJsonAsync<List<RemoteUser>>("/api/users");
        return Results.Ok(users);
    })
    .WithName("GetUsersFromWebApplication2");

app.MapGet("/api/remote-users/{id:int}", async (int id, IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient("WebApplication2");
        var response = await client.GetAsync($"/api/users/{id}");

        if (!response.IsSuccessStatusCode)
            return Results.StatusCode((int)response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<RemoteUser>();
        return Results.Ok(user);
    })
    .WithName("GetUserByIdFromWebApplication2");

app.MapGet("/api/users", async (AppDbContext dbContext) =>
        await dbContext.Users.ToListAsync())
    .WithName("GetUsers");

app.MapPost("/api/users", async (CreateUserRequest request, AppDbContext dbContext) =>
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/api/users/{user.Id}", user);
    })
    .WithName("CreateUser");

// ---- Redis okuma / yazma ----

app.MapPost("/api/cache", async (SetCacheRequest request, IConnectionMultiplexer redis) =>
    {
        var db = redis.GetDatabase();

        if (request.TtlSeconds is > 0)
            await db.StringSetAsync(request.Key, request.Value, TimeSpan.FromSeconds(request.TtlSeconds.Value));
        else
            await db.StringSetAsync(request.Key, request.Value);

        return Results.Ok(new { request.Key, request.Value, request.TtlSeconds });
    })
    .WithName("SetCacheValue");

app.MapGet("/api/cache/{key}", async (string key, IConnectionMultiplexer redis) =>
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        return value.HasValue
            ? Results.Ok(new { Key = key, Value = value.ToString() })
            : Results.NotFound(new { Key = key, Message = "Anahtar bulunamadi" });
    })
    .WithName("GetCacheValue");

app.MapDelete("/api/cache/{key}", async (string key, IConnectionMultiplexer redis) =>
    {
        var db = redis.GetDatabase();
        var removed = await db.KeyDeleteAsync(key);

        return removed ? Results.NoContent() : Results.NotFound();
    })
    .WithName("DeleteCacheValue");

// ---- RabbitMQ: kuyruga mesaj gonder (WebApplication2 dinliyor) ----

app.MapPost("/api/messages", async (SendMessageRequest request, IConnection rabbitConnection) =>
    {
        await using var channel = await rabbitConnection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "messages",
            durable: true,
            exclusive: false,
            autoDelete: false);

        var message = new QueueMessage(Guid.NewGuid(), request.Text, DateTime.UtcNow);
        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "messages", body: body);

        return Results.Accepted(value: message);
    })
    .WithName("SendMessageToQueue");

app.Run();

record Product(int Id, string Name, decimal Price, int Stock);

record RemoteUser(int Id, string Name, string Email);

record CreateUserRequest(string Name, string Email);

record SetCacheRequest(string Key, string Value, int? TtlSeconds);

record SendMessageRequest(string Text);

record QueueMessage(Guid Id, string Text, DateTime CreatedAtUtc);