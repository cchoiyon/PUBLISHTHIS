using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.InputModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Project3.WebApp.Controllers
{
    public partial class AccountController
    {
        [HttpGet("Register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Register the user using the AuthService
                    var result = await _authService.RegisterUserAsync(
                        model.Username, 
                        model.Email, 
                        model.Password, 
                        model.UserRole,
                        model.SecurityQuestion1, 
                        model.SecurityAnswer1,
                        model.SecurityQuestion2, 
                        model.SecurityAnswer2,
                        model.SecurityQuestion3, 
                        model.SecurityAnswer3
                    );
                    
                    if (result.Success)
                    {
                        // Store user info in TempData for the 2FA page
                        TempData["UserId"] = result.UserId;
                        TempData["Username"] = model.Username;
                        
                        // Ensure the role is properly normalized
                        string roleName = model.UserRole;
                        if (string.Equals(roleName, "reviewer", StringComparison.OrdinalIgnoreCase))
                        {
                            roleName = "Reviewer";
                        }
                        else if (string.Equals(roleName, "restaurantrep", StringComparison.OrdinalIgnoreCase))
                        {
                            roleName = "RestaurantRep";
                        }
                        
                        TempData["UserRole"] = roleName;
                        
                        // Inform the user that registration was successful but verification is needed
                        TempData["Message"] = "Registration successful! Please complete two-factor authentication to verify your account.";
                        
                        // Redirect to 2FA page
                        return RedirectToAction(nameof(LoginWith2fa));
                    }
                    else
                    {
                        // If there was a specific error message, add it to ModelState
                        if (!string.IsNullOrEmpty(result.ErrorMessage))
                        {
                            if (result.ErrorMessage.Contains("username"))
                            {
                                ModelState.AddModelError("Username", result.ErrorMessage);
                            }
                            else if (result.ErrorMessage.Contains("email"))
                            {
                                ModelState.AddModelError("Email", result.ErrorMessage);
                            }
                            else
                            {
                                ModelState.AddModelError("", result.ErrorMessage);
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Registration failed. Please try again.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during user registration for {Username}", model.Username);
                    ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                }
            }
            
            // If we got this far, something failed, redisplay form
            return View(model);
        }
    }
} 