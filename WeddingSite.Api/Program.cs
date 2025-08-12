using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WeddingSite.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    options.DefaultSignInScheme = IdentityConstants.BearerScheme;
})
    .AddBearerToken(IdentityConstants.BearerScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, config =>
    {
        // IMPORTANT: Configure the state data format to use SameSite=None
        config.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        config.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Required for SameSite=None
    })
    .AddCookie(IdentityConstants.ExternalScheme, config =>
    {
        // IMPORTANT: Configure the state data format to use SameSite=None
        config.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        config.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Required for SameSite=None
    })
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        googleOptions.Scope.Add("profile");
        googleOptions.SignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme;

        // IMPORTANT: Configure the state data format to use SameSite=None
        googleOptions.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
        googleOptions.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // Required for SameSite=None
    });

builder.Services.ConfigureApplicationCookie(options =>
    {

        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.None;

    });

builder.Services.AddAuthorization(config =>
{
    config.DefaultPolicy = new AuthorizationPolicyBuilder()
       .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme)
       .RequireAuthenticatedUser()
       .Build();
});

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


app.UseSwagger();
app.UseSwaggerUI();

// Configure only in prod.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // This is to avoid problems with azure. See https://pellerex.com/blog/google-auth-for-react-with-aspnet-identity and https://github.com/dotnet/AspNetCore.Docs/issues/14169
    //app.Use((context, next) =>
    //{
    //    context.Request.Host = new HostString("weddingsiteapi-gtadckbkbkh2fhe4.westeurope-01.azurewebsites.net");
    //    context.Request.Scheme = "https";
    //    return next();
    //});
}



// Make sure you use app.UseCookiePolicy() in your pipeline
app.UseCookiePolicy();

app.UseForwardedHeaders();

app.UseCors("AllowNextJSClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapIdentityApi<ApplicationUser>();

app.MapGet("/", () =>
{
    return "Welecome to the WeddingSite API of Simone Vuotto";
}).AllowAnonymous();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

if (!string.IsNullOrEmpty(port))
{
    var url = $"http://0.0.0.0:{port}";

    app.Run(url);
} else
{
    app.Run();
}


