using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WeddingSite.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

// Add DB
var connectionString = builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Connection string 'Database' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJSClient", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://wedding-site-git-main-simov8s-projects.vercel.app/",
            "https://wedding-site-r0z7enkw0-simov8s-projects.vercel.app", "https://wedding-site-blond-tau.vercel.app/")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Identity Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddBearerToken(IdentityConstants.BearerScheme)
    .AddCookie(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ExternalScheme)
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        // googleOptions.CallbackPath = "/User/google-callback";

        googleOptions.Scope.Add("profile");
        googleOptions.SignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme;

        // IMPORTANT: Configure the state data format to use SameSite=None
        googleOptions.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        googleOptions.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Required for SameSite=None
    });

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddApiEndpoints();

// Also, ensure your overall cookie policy allows SameSite=None
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
    options.Secure = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Ensures cookies are sent only over HTTPS
});


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Add any known proxies if applicable. For localhost, this might not be strictly needed unless using specific tools.
    // options.KnownProxies.Add(IPAddress.Parse("YOUR_PROXY_IP"));
});



// Add HttpClient for Google token verification
builder.Services.AddHttpClient();

var app = builder.Build();

app.Logger.LogCritical("Application started");
// Configure the HTTP request pipeline.

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Website API");
});

app.UseHttpsRedirection();

// Make sure you use app.UseCookiePolicy() in your pipeline
app.UseCookiePolicy();

app.UseForwardedHeaders();

app.UseCors("AllowNextJSClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<ApplicationUser>();

app.Run();

