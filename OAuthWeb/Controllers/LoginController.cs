using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using OAuthWeb.Models;

namespace OAuthWeb.Controllers;

public class LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        // 1. Discovery endpoint'inden token endpoint'ini al.
        var tokenClient = httpClientFactory.CreateClient("TokenClient");

        var disco = await tokenClient.GetDiscoveryDocumentAsync(configuration["Keycloak:Authority"]);
        if (disco.IsError)
        {
            model.ErrorMessage = $"Discovery document alınamadı: {disco.Error}";
            return View(model);
        }
        
        // 2. Resource Owner Password Credentials akışı ile token al.
        var tokenResponse = await tokenClient.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = configuration["Keycloak:ClientId"]!,
            ClientSecret = configuration["Keycloak:ClientSecret"]!,
            UserName = model.Username,
            Password = model.Password,
            Scope = "openid"
        });

        if (tokenResponse.IsError)
        {
            model.ErrorMessage = $"Token alınamadı: {tokenResponse.Error}";
            return View(model);
        }

        model.AccessToken = tokenResponse.AccessToken;

        // 3. Alınan token ile Microservice1 /api/names adresine istek at.
        var apiClient = httpClientFactory.CreateClient("Microservice1");
        apiClient.SetBearerToken(tokenResponse.AccessToken!);

        var apiResponse = await apiClient.GetAsync("/api/names");
        if (!apiResponse.IsSuccessStatusCode)
        {
            model.ErrorMessage = $"Microservice1 isteği başarısız: {(int)apiResponse.StatusCode} {apiResponse.ReasonPhrase}";
            return View(model);
        }

        model.Names = await apiResponse.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        return View(model);
    }
}
