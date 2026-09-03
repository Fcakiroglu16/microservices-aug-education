using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   
}

app.MapOpenApi();
app.MapScalarApiReference();





var products = new List<Product>
{
    new(1, "Laptop", 45000m, 10),
    new(2, "Mouse", 750m, 150),
    new(3, "Keyboard", 1250m, 80),
    new(4, "Monitor", 12500m, 25)
};

app.MapGet("/api/products", () => products)
    .WithName("GetProducts");

app.MapGet("/api/products/{id:int}", (int id) =>
        products.FirstOrDefault(p => p.Id == id) is { } product
            ? Results.Ok(product)
            : Results.NotFound())
    .WithName("GetProductById");

var users = new List<User>
{
    new(1, "Ahmet Yilmaz", "ahmet@example.com"),
    new(2, "Ayse Demir", "ayse@example.com"),
    new(3, "Mehmet Kaya", "mehmet@example.com")
};

app.MapGet("/api/users", () => users)
    .WithName("GetUsers");

app.MapGet("/api/users/{id:int}", (int id) =>
        users.FirstOrDefault(u => u.Id == id) is { } user
            ? Results.Ok(user)
            : Results.NotFound())
    .WithName("GetUserById");

app.Run();


record Product(int Id, string Name, decimal Price, int Stock);

record User(int Id, string Name, string Email);