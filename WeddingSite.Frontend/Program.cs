using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WeddingSite.Components;
using WeddingSite.Components.Account;
using WeddingSite.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// --- 4. Add Google Authentication Service ---
// This is the core configuration for Google authentication.
builder.Services.AddAuthentication(options =>
{
    // Set the default authentication scheme. This tells ASP.NET Core
    // which scheme to use for general authentication operations (e.g., checking if a user is logged in).
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Set the default challenge scheme. This scheme is used when an unauthenticated user
    // tries to access a protected resource and needs to be redirected to a login provider.
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie() // Adds cookie-based authentication for local logins and external login callbacks
.AddGoogle(googleOptions =>
{
    // Retrieve Client ID and Client Secret from configuration (e.g., User Secrets or appsettings.json).
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    // Optional: Request additional user information (scopes).
    // By default, 'profile' and 'email' scopes are often included.
    // googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
    // googleOptions.Scope.Add("https://www.googleapis.com/auth/userinfo.email");

    // Optional: Save tokens (access token, refresh token) after successful authentication.
    // This is needed if you plan to call Google APIs on behalf of the user later.
    // googleOptions.SaveTokens = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// --- 5. Add Authentication and Authorization Middleware ---
// These two lines are crucial and must be placed after UseRouting() and before MapControllerRoute/MapRazorPages.
app.UseAuthentication(); // Enables authentication features
app.UseAuthorization();  // Enables authorization features

app.Run();
