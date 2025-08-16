using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using WeddingSite.Api.Data;
using WeddingSite.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "api.vanessaesimone.it",
            Version = "v1"
        }
     );

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), true);
});



// Add DB
var connectionString = builder.Configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Connection string 'Database' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJSClient", policy =>
    {
        policy.WithOrigins(Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(";") ?? ["http://localhost:3000"])
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Identity Authentication - JWT tokens only
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero // no delay on token expiration
        };
    });

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserDBLog, UserDBLog>();

builder.Services.AddAuthorization(config =>
{
    config.DefaultPolicy = new AuthorizationPolicyBuilder()
       .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
       .RequireAuthenticatedUser()
       .Build();
});

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddApiEndpoints();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
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
}

app.UseForwardedHeaders();

app.UseCors("AllowNextJSClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapGet("/", () =>
{
    return "Welecome to the WeddingSite API of Simone Vuotto!";
}).AllowAnonymous();

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrEmpty(port))
{
    var url = $"http://0.0.0.0:{port}";

    app.Run(url);
}
else
{
    app.Run();
}


