namespace ObservabilityTwo.API;

public class ProductService(ILogger<ProductService> logger)
{
    public List<Product> GetProducts()
    {

        var userId = 123;
        var orderId = 456;
        logger.LogInformation($"Getting products for user {userId}");
        
        logger.LogInformation("Getting products for user {UserId} and order {OrderId}", userId, orderId);
        return new List<Product>
        {
            new Product { Id = 1, Name = "Product 1", Price = 10.99m },
            new Product { Id = 2, Name = "Product 2", Price = 19.99m },
            new Product { Id = 3, Name = "Product 3", Price = 29.99m }
        };
    }
}