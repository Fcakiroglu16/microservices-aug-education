namespace OAuthWeb.Models;

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public List<string> Names { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
}
