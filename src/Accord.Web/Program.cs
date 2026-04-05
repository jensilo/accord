using System.Security.Claims;
using Accord.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using Accord.Web.Infrastructure.Data;
using Accord.Web.Infrastructure.Seeding;
using Accord.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<MailOptions>(builder.Configuration.GetSection("Mail"));
builder.Services.Configure<TemplateOptions>(builder.Configuration.GetSection("Templates"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IRequirementService, RequirementService>();
builder.Services.AddScoped<TemplateSeeder>();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts =>
    {
        opts.Cookie.Name = builder.Configuration["Auth:CookieName"] ?? "accord.session";
        opts.Cookie.HttpOnly = true;
        opts.Cookie.SameSite = SameSiteMode.Lax;
        opts.LoginPath = "/auth/login";
        opts.LogoutPath = "/auth/logout";
        opts.AccessDeniedPath = "/auth/login";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Trust X-Forwarded-Proto/For from ACA's ingress proxy so that
    // HTTPS scheme is visible to the app (magic link URLs, HSTS, etc.)
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);

    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

var supportedCultures = new[] { "de", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("de")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/auth/verify", async (string token, IAuthService authService, HttpContext http) =>
{
    var user = await authService.ValidateAndConsumeToken(token);
    if (user == null)
        return Results.Redirect("/auth/login");

    var claims = new Claim[]
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var props = new AuthenticationProperties { IsPersistent = true };
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);
    return Results.Redirect("/");
});

app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapGet("/culture/set", (string culture, string redirectUri, HttpContext http) =>
{
    var supported = new[] { "de", "en" };
    if (!supported.Contains(culture)) return Results.BadRequest();
    http.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
    );
    return Results.LocalRedirect(redirectUri);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<TemplateSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Startup initialization failed");
    }
}

app.Run();
