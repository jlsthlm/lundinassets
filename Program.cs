using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LundinAssets.Components;
using LundinAssets.Data;
using LundinAssets.Models;
using LundinAssets.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// Database
// Empty as well as missing: appsettings.json ships a blank connection string so no
// credentials live in the repo, so an unconfigured deploy would otherwise fail later
// with an opaque MySQL error instead of pointing at the real problem.
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connStr))
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. Set it via the " +
        "ConnectionStrings__DefaultConnection environment variable (docker-compose.yml " +
        "reads DB_* from .env; see .env.example).");
// Pin the server version rather than using ServerVersion.AutoDetect(), which opens a
// connection here — before the app is built — and crashes the process if MySQL isn't
// up yet. depends_on/service_healthy does not protect this: it only applies to
// "docker compose up", and is bypassed when the Docker daemon restarts containers on
// boot. The db service is pinned to mysql:8.0 in docker-compose.yml, and
// DbInitializer already retries the first real connection, so startup now tolerates
// the database coming up late.
var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connStr, serverVersion));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider>();

// App Services
builder.Services.AddScoped<AssetService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<FileUploadService>();

// HttpContext for Identity sign-in in Blazor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Identity form endpoints for static SSR login/logout
app.MapPost("/account/login-action", async (
    HttpContext ctx,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";
    var returnUrl = form["returnUrl"].ToString();

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, false);
    if (result.Succeeded)
        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);

    return Results.Redirect($"/account/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
}).DisableAntiforgery();

app.MapPost("/account/logout-action", async (
    HttpContext ctx,
    SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
}).DisableAntiforgery();

app.Run();
