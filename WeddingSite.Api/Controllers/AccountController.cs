using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WeddingSite.Api.Data;

namespace WeddingSite.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger; // Good practice for logging

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // This action is typically invoked by a form post from your login page
        // when a user clicks an "External Login" button (e.g., "Login with Google").
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            // The 'redirectUrl' is where Google will send the user back after authentication.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });

            // Configure properties for the external authentication challenge.
            // This is where the provider (e.g., "Google") and the redirect URL are set.
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            // Challenge the authentication scheme. This triggers the redirect to Google.
            return new ChallengeResult(provider, properties);
        }

        // This action is the callback endpoint where Google redirects the user after authentication.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            // Handle any errors returned by the external provider (e.g., user denied consent).
            if (remoteError != null)
            {
                _logger.LogError($"Error from external provider: {remoteError}");
                return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl });
            }

            // --- 1. Get External Login Information ---
            // This retrieves the claims and provider details from the external authentication cookie
            // that was set by the Google authentication middleware after the redirect from Google.
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Error loading external login information.");
                return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl });
            }

            // --- 2. Attempt to Sign In Existing User with External Login ---
            // This method checks the AspNetUserLogins table.
            // If a record exists matching info.LoginProvider and info.ProviderKey,
            // it means this Google account is already linked to an existing user in your system.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // Scenario A: User already exists and is linked to this Google account.
                // Sign-in successful. Log and redirect.
                _logger.LogInformation($"User logged in with {info.LoginProvider} provider.");
                return LocalRedirect(returnUrl ?? "~/"); // Redirect to the original requested URL or home
            }
            if (result.IsLockedOut)
            {
                // User account is locked out.
                _logger.LogWarning($"User account locked out for {info.LoginProvider} provider.");
                return RedirectToPage("/Account/Lockout");
            }
            if (result.IsNotAllowed)
            {
                // User is not allowed to sign in (e.g., email not confirmed).
                _logger.LogWarning($"User not allowed to sign in with {info.LoginProvider} provider.");
                return RedirectToPage("/Account/AccessDenied"); // Or a specific "Not Allowed" page
            }
            else
            {
                // Scenario B & C: User does NOT have an existing linked external login.
                // This means it's either a new user, or an existing local user
                // who is logging in with Google for the first time.


                // Extract email from claims provided by Google.
                // The 'email' claim is typically available if 'email' scope was requested.
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                // --- 3. Check for Existing User by Email (Scenario B) ---
                // Before creating a new user, check if a user with the same email already exists
                // in your local Identity system (e.g., they registered with username/password).
                var user = await _userManager.FindByEmailAsync(email);

                if (user != null)
                {
                    // Scenario B: Existing local user without external login.
                    // Link the Google account to the existing local user.
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (addLoginResult.Succeeded)
                    {
                        // Link successful. Now sign in the user.
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation($"External login '{info.LoginProvider}' added to existing user '{user.Email}'.");
                        return LocalRedirect(returnUrl ?? "~/");
                    }
                    else
                    {
                        // Failed to add login (e.g., login already exists for another user, though unlikely with ProviderKey)
                        _logger.LogError($"Failed to add external login for existing user '{user.Email}': {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                        return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl });
                    }
                }
                else
                {
                    // Scenario C: New User (no existing local account or linked external login).
                    // Prepare to create a new user account and link the external login.
                    // This often redirects to a "Confirmation" page/view to allow the user
                    // to confirm their email or provide additional details before creating the account.

                    // You might pass the email and other claims to the confirmation view/page.
                    // For a simple setup, you could auto-create the user here without confirmation.
                    // Example of auto-creation:
                    var newUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true }; // Set EmailConfirmed based on your policy
                    var createUserResult = await _userManager.CreateAsync(newUser);

                    if (createUserResult.Succeeded)
                    {
                        // Add the external login to the newly created user.
                        var addLoginResult = await _userManager.AddLoginAsync(newUser, info);
                        if (addLoginResult.Succeeded)
                        {
                            // New user created and linked. Sign them in.
                            await _signInManager.SignInAsync(newUser, isPersistent: false);
                            _logger.LogInformation($"New user '{newUser.Email}' created with external login '{info.LoginProvider}'.");
                            return LocalRedirect(returnUrl ?? "~/");
                        }
                        else
                        {
                            // Failed to add login for new user. Clean up the created user.
                            await _userManager.DeleteAsync(newUser);
                            _logger.LogError($"Failed to add external login for new user '{newUser.Email}': {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                            return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl });
                        }
                    }
                    else
                    {
                        // Failed to create new user.
                        _logger.LogError($"Failed to create new user with email '{email}': {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
                        return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl });
                    }

                    // If you prefer a confirmation page for new users:
                    // return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
                }
            }
        }
    }
}
