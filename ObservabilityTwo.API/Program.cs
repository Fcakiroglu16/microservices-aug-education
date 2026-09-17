using System.Diagnostics;
using ObservabilityTwo.API;

var builder = WebApplication.CreateBuilder(args);
var applicationName = builder.Environment.ApplicationName;
ActivitySourceProvider._activitySource = new ActivitySource(applicationName);
builder.AddServiceDefaults();
builder.AddRabbitMQClient("rabbitmq");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<ProductService>();
builder.Services.AddHostedService<UserCreatedEventConsumer>();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.MapGet("/api/products", (ProductService productService) =>
{
    var products = productService.GetProducts();
    return Results.Ok(products);
});
    


app.Run();

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

