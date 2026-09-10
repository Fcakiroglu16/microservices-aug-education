using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
    JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.Authority = "https://localhost:8080/realms/mycompany";
        opts.RequireHttpsMetadata = true;
        opts.Audience = "microservice-1";
        
        opts.TokenValidationParameters=new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,
                ValidAudience = "microservice-1",
                ValidateIssuer = true,
                ValidIssuer = "https://localhost:8080/realms/mycompany",
                ValidateLifetime = true,
            };


    }).AddJwtBearer(
    "BranchSchema", opts =>
    {
        opts.Authority = "https://localhost:8080/realms/mycompany";
        opts.RequireHttpsMetadata = false;
        opts.Audience = "exchange.api";


    });

builder.Services.AddAuthorization();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/names", () =>
{
    var names = new List<string> { "Alice", "Bob", "Charlie", "David", "Eve" };
    return Results.Ok(names);
}).RequireAuthorization();

app.Run();

