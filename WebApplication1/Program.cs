using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WebApplication1.Data;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("WebApplication2", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:WebApplication2"]!);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

var app = builder.Build();

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

app.Run();

record Product(int Id, string Name, decimal Price, int Stock);

record CreateUserRequest(string Name, string Email);