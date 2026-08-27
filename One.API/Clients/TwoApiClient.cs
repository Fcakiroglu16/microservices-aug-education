using SharedLibrary;

namespace One.API.Clients;

// HttpClient DI Container'dan (AddHttpClient => IHttpClientFactory) geliyor.
public class TwoApiClient(HttpClient httpClient, ILogger<TwoApiClient> logger)
{
    public async Task<List<ProductDto>?> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Requesting products from Two.API => {BaseAddress}", httpClient.BaseAddress);

        var response = await httpClient.GetAsync("api/products", cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<ProductDto>>(cancellationToken);
    }
}
