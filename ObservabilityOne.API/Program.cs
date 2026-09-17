var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

app.Run();

record ProductDto(int Id, string Name, decimal Price);

