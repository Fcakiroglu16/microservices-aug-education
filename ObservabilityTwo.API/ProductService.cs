using System.Diagnostics;
using OpenTelemetry.Trace;

namespace ObservabilityTwo.API;

public class ProductService(ILogger<ProductService> logger)
{
    public List<Product> GetProducts()
    {
        
        
        
        using var activity = ActivitySourceProvider._activitySource?.StartActivity("GetProductsX", ActivityKind.Server);
       
        activity.AddEvent(new ActivityEvent("GetProducts start"));
        
        var userId = 123;
        var orderId = 456;
      
        activity?.AddTag("userId", userId);
        activity?.AddTag("orderId", orderId);
        
        
      Thread.Sleep(2000);
        
        logger.LogInformation($"Getting products for user {userId}");
        Thread.Sleep(1000);
        logger.LogInformation("Getting products for user {UserId} and order {OrderId}", userId, orderId);
      
        activity.AddEvent(new ActivityEvent("GetProducts end"));
        return new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", Price = 10.99m },
            new Product { Id = 2, Name = "Product 2", Price = 19.99m },
            new Product { Id = 3, Name = "Product 3", Price = 29.99m }
        };
    
        
     
    }
}