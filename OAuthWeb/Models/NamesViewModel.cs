namespace OAuthWeb.Models;

public class NamesViewModel
{
    public List<string> Names { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
}
