using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json;
using WeddingSite.Api.Data;
using WeddingSite.Api.Models;
using WeddingSite.Api.Services;
using RegisterRequest = WeddingSite.Api.Models.RegisterRequest;

namespace WeddingSite.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ILogger<UserController> logger;
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly ITokenService tokenService;

        public UserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            ILogger<UserController> logger, HttpClient httpClient, IConfiguration configuration, ITokenService tokenService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.httpClient = httpClient;
            this.configuration = configuration;
            this.tokenService = tokenService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return Ok(new RegisterResponse
                {
                    Id = user.Id,
                    UserName = user.UserName!,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Message = "User registered successfully"
                });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest("Invalid credentials");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                Results.BadRequest("Invalid credentials");
            }

            var token = tokenService.GenerateTokens(user);
            return Ok(token);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            var user = tokenService.ValidateRefreshToken(request.RefreshToken);

            // validate refresh token
            if (user == null)
            {
                return BadRequest("Invalid refresh token");
            }

            var tokens = tokenService.GenerateTokens(user);
            return Ok(tokens);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest("User not found");
            }
            tokenService.RevokeRefreshToken(user.Id);
            return Ok();
        }


        [HttpGet("info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled
            });
        }

        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var clientId = configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest("Google OAuth not configured");
            }

            var redirectUri = $"{Request.Scheme}://{Request.Host}/User/google-callback";
            var state = returnUrl ?? string.Empty; // Pass returnUrl as state parameter

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={Uri.EscapeDataString(clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                         $"response_type=code&" +
                         $"scope={Uri.EscapeDataString("openid email profile")}&" +
                         $"state={Uri.EscapeDataString(state)}";

            logger.LogInformation($"Redirecting to Google OAuth: {authUrl}");
            return Redirect(authUrl);
        }


        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback(string? code = null, string? error = null, string? state = null)
        {
            var returnUrl = !string.IsNullOrEmpty(state) ? state :
                Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(";")[0] ?? "http://localhost:3000";

            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError($"Google OAuth error: {error}");
                return Redirect($"{returnUrl}/login?error={Uri.EscapeDataString(error)}");
            }

            if (string.IsNullOrEmpty(code))
            {
                logger.LogError("No authorization code received from Google");
                return Redirect($"{returnUrl}/login?error=no_code");
            }

            try
            {
                logger.LogInformation("Processing Google OAuth callback");

                // Exchange authorization code for access token
                var redirectUri = $"{Request.Scheme}://{Request.Host}/User/google-callback";
                var tokenResponse = await ExchangeCodeForTokens(code, redirectUri);
                if (tokenResponse == null)
                {
                    logger.LogError("Failed to exchange authorization code for access token");
                    return Redirect($"{returnUrl}/login?error=token_exchange_failed");
                }

                // Get user info from Google
                var userInfo = await GetGoogleUserInfo(tokenResponse.AccessToken);
                if (userInfo == null)
                {
                    logger.LogError("Failed to get user info from Google");
                    return Redirect($"{returnUrl}/login?error=user_info_failed");
                }

                // Find or create user
                var user = await FindOrCreateUser(userInfo);
                if (user == null)
                {
                    logger.LogError("Failed to create or find user");
                    return Redirect($"{returnUrl}/login?error=user_creation_failed");
                }

                logger.LogInformation($"User authentication successful: {user.Email}");


                var token = tokenService.GenerateTokens(user);

                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                };

                var json = JsonConvert.SerializeObject(token, settings);

                // Redirect to frontend with JWT token
                return Redirect($"{returnUrl}#token={Uri.EscapeDataString(json)}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Google OAuth callback processing");
                return Redirect($"{returnUrl}/login?error=authentication_failed");
            }
        }

        // This model matches what Identity encodes in its bearer token
        internal record AccessTokenPayload(string UserId, DateTimeOffset Expires);

        private static string GenerateIdentityBearerToken(ApplicationUser user, TimeSpan lifetime, IDataProtectionProvider provider)
        {
            var payload = new AccessTokenPayload(user.Id, DateTimeOffset.UtcNow.Add(lifetime));
            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            var protector = provider.CreateProtector("Identity.BearerToken");
            return protector.Protect(json);
        }

        private async Task<GoogleTokenResponse?> ExchangeCodeForTokens(string code, string redirectUri)
        {
            var clientId = configuration["Authentication:Google:ClientId"];
            var clientSecret = configuration["Authentication:Google:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                logger.LogError("Google OAuth credentials not configured");
                return null;
            }

            var tokenRequest = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            };

            var requestContent = new FormUrlEncodedContent(tokenRequest);
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError($"Google token exchange failed: {response.StatusCode} - {error}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(jsonResponse);
        }

        private async Task<GoogleUserInfo?> GetGoogleUserInfo(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError($"Google user info request failed: {response.StatusCode} - {error}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(jsonResponse);
        }

        private async Task<ApplicationUser?> FindOrCreateUser(GoogleUserInfo googleUser)
        {
            if (string.IsNullOrEmpty(googleUser.Email))
            {
                logger.LogError("Google user info missing email");
                return null;
            }

            // Try to find existing user
            var existingUser = await userManager.FindByEmailAsync(googleUser.Email);
            if (existingUser != null)
            {
                logger.LogInformation($"Found existing user: {existingUser.Email}");
                return existingUser;
            }

            // Create new user
            var fullName = !string.IsNullOrEmpty(googleUser.Name)
                ? googleUser.Name
                : googleUser.Email.Split('@')[0];

            var newUser = new ApplicationUser
            {
                UserName = googleUser.Email,
                Email = googleUser.Email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                logger.LogInformation($"Created new user: {newUser.Email}");
                return newUser;
            }

            logger.LogError($"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            return null;
        }

    }
}