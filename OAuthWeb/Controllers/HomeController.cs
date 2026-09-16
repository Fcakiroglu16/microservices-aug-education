using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAuthWeb.Models;

namespace OAuthWeb.Controllers;


public class HomeController : Controller
{
    public IActionResult Index()
    {
        
        return View();
    }

    [Authorize]
    public IActionResult Privacy()
    {
        
        var userName= User.Identity?.Name;
        
        var accessToken=HttpContext.GetTokenAsync("access_token").Result;
        var idToken=HttpContext.GetTokenAsync("id_token").Result;
        var refreshToken=HttpContext.GetTokenAsync("refresh_token").Result;
        
        
         var claims = User.Claims;
        
        return View();
    }
    
    public IActionResult Logout()
    {
        return SignOut("Cookies","oidc");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}