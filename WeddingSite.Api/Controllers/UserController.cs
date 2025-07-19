using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeddingSite.Api.Data;
using WeddingSite.Api.Models;

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

        public UserController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<UserController> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
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
        public IActionResult GoogleLogin(string returnUrl = null)
        {
            //var redirectUrl = Url.Action(nameof(GoogleLoginCallback), "User", new { returnUrl });
            //var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            //return new ChallengeResult("Google", properties);
            var redirectUrl = $"https://localhost:3001/User/google-callback?returnUrl={returnUrl}";
            var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            properties.AllowRefresh = true;
            return Challenge(properties, "Google");
        }

        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                logger.LogError($"Error from Google: {remoteError}");
                return Redirect($"http://localhost:3000/login?error={Uri.EscapeDataString(remoteError)}");
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                logger.LogError("Error loading external login information from Google");
                return Redirect("http://localhost:3000/login?error=external_login_failed");
            }

            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                logger.LogInformation($"User logged in with {info.LoginProvider} provider");
                return Redirect("http://localhost:3000/");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                logger.LogError("Email claim not found from Google");
                return Redirect("http://localhost:3000/login?error=email_not_found");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var addLoginResult = await userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    logger.LogInformation($"External login '{info.LoginProvider}' added to existing user '{user.Email}'");
                    return Redirect("http://localhost:3000/");
                }
                else
                {
                    logger.LogError($"Failed to add external login for existing user '{user.Email}': {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                    return Redirect("http://localhost:3000/login?error=link_failed");
                }
            }
            else
            {
                var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? 
                               info.Principal.FindFirstValue("name") ?? 
                               email.Split('@')[0];
                
                var newUser = new ApplicationUser 
                { 
                    UserName = email, 
                    Email = email, 
                    EmailConfirmed = true,
                    FullName = fullName
                };

                var createUserResult = await userManager.CreateAsync(newUser);
                if (createUserResult.Succeeded)
                {
                    var addLoginResult = await userManager.AddLoginAsync(newUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        await signInManager.SignInAsync(newUser, isPersistent: false);
                        logger.LogInformation($"New user '{newUser.Email}' created with external login '{info.LoginProvider}'");
                        return Redirect("http://localhost:3000/");
                    }
                    else
                    {
                        await userManager.DeleteAsync(newUser);
                        logger.LogError($"Failed to add external login for new user '{newUser.Email}': {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                        return Redirect("http://localhost:3000/login?error=create_failed");
                    }
                }
                else
                {
                    logger.LogError($"Failed to create new user with email '{email}': {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
                    return Redirect("http://localhost:3000/login?error=create_failed");
                }
            }
        }
    }
}