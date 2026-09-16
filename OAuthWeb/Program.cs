using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment
        .ContentRootPath, "DataProtectionKeys"))).SetDefaultKeyLifetime(TimeSpan.FromDays(14));
// Add services to the container.
builder.Services.AddControllersWithViews();


builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }).AddCookie(opts =>
    {
        
    })
    .AddOpenIdConnect(opts =>
    {
        opts.RequireHttpsMetadata = false;

        opts.Authority = "https://localhost:8080/realms/mycompany";
        opts.ClientId = "web";
        opts.ClientSecret = "z3mujf0d6FpfQT0prSz2LGUxmh31LEmi";
        opts.ResponseType = "code";
        opts.GetClaimsFromUserInfoEndpoint = true;
        opts.SaveTokens = true;
        opts.Scope.Add("profile email address phone roles");
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };

      
    });








// Named HttpClient for token requests and for calling Microservice1.
builder.Services.AddHttpClient("TokenClient");
builder.Services.AddHttpClient("Microservice1", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Microservice1:BaseUrl"]!);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();