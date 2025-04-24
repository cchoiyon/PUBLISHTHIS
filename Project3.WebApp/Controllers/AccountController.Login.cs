using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project3.Shared.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Project3.WebApp.Models;

namespace Project3.WebApp.Controllers
{
    public partial class AccountController
    {
        // TwoFactorViewModel to replace TwoFactorModel
        public class TwoFactorViewModel
        {
            public string TwoFactorCode { get; set; }
            public bool RememberMe { get; set; }
        }

        // --- Login (GET) ---
        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null, bool showSecurityQuestionReset = false)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            // If user is already logged in, redirect them away from the login page
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard();
            }
            
            // Pass any success message from TempData
            ViewBag.SuccessMessage = TempData["Message"];
            
            // If security question reset was requested, set the ViewData flag
            if (showSecurityQuestionReset)
            {
                ViewData["ShowSecurityQuestionForm"] = true;
                ViewData["SecurityQuestionModel"] = new Project3.Shared.Models.InputModels.SecurityQuestionResetModel();
            }
            
            return View();
        }

        // --- Login (POST) ---
        [HttpPost("Login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate credentials
                    var user = await _authService.ValidateCredentials(model.Username, model.Password);
                    
                    if (user != null)
                    {
                        if (user.IsVerified)
                        {
                            // User is verified, log them in directly
                            await _authService.SignInAsync(HttpContext, user, model.RememberMe);
                            
                            _logger.LogInformation("Verified user {Username} logged in successfully", model.Username);
                            return RedirectToDashboard();
                        }
                        else
                        {
                            // Generate and send 2FA code
                            await _authService.Generate2FACodeAsync(user);
                            
                            // Store user info in TempData for the 2FA page
                            TempData["UserId"] = user.UserId;
                            TempData["Username"] = user.Username;
                            TempData["UserRole"] = user.UserType;
                            
                            // Redirect to 2FA page
                            return RedirectToAction(nameof(LoginWith2fa), new { returnUrl });
                        }
                    }
                    
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login attempt");
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }
            
            return View(model);
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("LoginWith2fa")]
        [AllowAnonymous]
        public IActionResult LoginWith2fa(string returnUrl = null)
        {
            // Make sure the user has gone through the first login page or registration
            if (TempData["UserId"] == null)
            {
                return RedirectToAction(nameof(Login));
            }
            
            // Keep the returnUrl and UserId for the post action
            ViewData["ReturnUrl"] = returnUrl;
            TempData.Keep("UserId");
            TempData.Keep("Username");
            TempData.Keep("UserRole");
            
            string username = TempData["Username"]?.ToString();
            ViewBag.Username = username; // Pass to view for personalized message
            
            // Set a helpful message depending on where the user came from
            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"];
                TempData.Keep("Message");
            }
            else
            {
                ViewBag.Message = "Please enter the verification code that was sent to your email.";
            }
            
            // Add extra info to help the user
            ViewBag.InfoMessage = "A 6-digit verification code has been sent to your email. " +
                                 "Please check your inbox (and spam folder) and enter the code below to verify your account.";
            
            return View();
        }

        [HttpPost("LoginWith2fa")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(TwoFactorViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (TempData["UserId"] == null)
            {
                return RedirectToAction(nameof(Login));
            }
            
            // Keep values in TempData in case we need to redisplay the form
            TempData.Keep("UserId");
            TempData.Keep("Username");
            TempData.Keep("UserRole");
            
            if (ModelState.IsValid)
            {
                int userId = Convert.ToInt32(TempData["UserId"]);
                string username = TempData["Username"]?.ToString();
                string userRole = TempData["UserRole"]?.ToString();
                
                // Verify the 2FA code
                if (await _authService.Verify2FACodeAsync(userId, model.TwoFactorCode))
                {
                    // Mark user as verified
                    await _authService.ConfirmVerificationAsync(userId);
                    
                    // Create user data for sign in
                    var user = await _userRepository.GetUserById(userId);
                    
                    if (user != null)
                    {
                        // Sign in user
                        await _authService.SignInAsync(HttpContext, user, model.RememberMe);
                        
                        TempData["Message"] = "Your account has been verified successfully. Welcome!";
                        return RedirectToDashboard();
                    }
                }
                
                ModelState.AddModelError("TwoFactorCode", "Invalid verification code. Please try again.");
            }
            
            return View(model);
        }

        [HttpGet("Cancel2FA")]
        [AllowAnonymous]
        public IActionResult Cancel2FA()
        {
            // Clear any 2FA data from TempData
            TempData.Remove("UserId");
            TempData.Remove("Username");
            TempData.Remove("UserRole");
            
            // Redirect back to login
            return RedirectToAction(nameof(Login));
        }
    }
} 