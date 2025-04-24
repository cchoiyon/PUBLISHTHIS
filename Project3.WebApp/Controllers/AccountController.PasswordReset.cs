using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.InputModels;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Project3.WebApp.Models;

namespace Project3.WebApp.Controllers
{
    public partial class AccountController
    {
        [HttpGet("ForgotPassword")]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            // Present both email and security question options
            ViewBag.Message = "You can reset your password using either your email or security questions.";
            return View();
        }

        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Remind users about security questions option
                    ViewBag.SecurityQuestionOption = "If you can't access your email, try resetting your password with security questions instead.";
                    ViewBag.SecurityQuestionLink = Url.Action("SecurityQuestionReset", "Account");
                    
                    // Generate password reset token
                    bool success = await _authService.GeneratePasswordResetAsync(model.EmailOrUsername);
                    
                    // Always redirect to confirmation page (for security)
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during forgot password process");
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                }
            }
            
            return View(model);
        }

        [HttpGet("ForgotPasswordConfirmation")]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet("ResetPassword")]
        [AllowAnonymous]
        public IActionResult ResetPassword(int userId, string token = null)
        {
            bool isFromSecurityQuestion = string.IsNullOrEmpty(token);
            
            var model = new ResetPasswordModel
            {
                UserId = userId.ToString(),
                Token = token,
                ResetFromSecurityQuestion = isFromSecurityQuestion
            };
            
            return View(model);
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            // If we're coming from security question verification, we don't need a token
            if (model.ResetFromSecurityQuestion)
            {
                ModelState.Remove("Token");
            }
            
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "New password is required.");
                return View(model);
            }
            
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
                return View(model);
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Parse user ID
                    if (int.TryParse(model.UserId, out int userId))
                    {
                        // Reset the password
                        bool success = await _authService.ResetPasswordAsync(userId, model.NewPassword);
                        
                        if (success)
                        {
                            TempData["Message"] = "Your password has been reset successfully. You can now log in with your new password.";
                        }
                        else
                        {
                            TempData["Message"] = "Password reset completed. Please try logging in with your new password.";
                        }
                        
                        return RedirectToAction(nameof(Login));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during password reset");
                    ModelState.AddModelError("", $"An error occurred during password reset: {ex.Message}");
                }
            }
            
            // Redirect to login with a generic message for security
            TempData["Message"] = "The password reset process has been completed. Please try logging in with your credentials.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("SecurityQuestionReset")]
        [AllowAnonymous]
        public IActionResult SecurityQuestionReset()
        {
            // Redirect to login page with flag to show security question form
            return RedirectToAction(nameof(Login), new { showSecurityQuestionReset = true });
        }

        [HttpPost("SecurityQuestionReset")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SecurityQuestionReset(Project3.Shared.Models.InputModels.SecurityQuestionResetModel model)
        {
            if (string.IsNullOrEmpty(model.Username))
            {
                ModelState.AddModelError("Username", "Username is required");
                ViewData["ShowSecurityQuestionForm"] = true;
                ViewData["SecurityQuestionModel"] = model;
                return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
            }
            
            // Determine which question/answer pair we're validating
            int questionNumber = 0;
            string answerToCheck = null;
            
            if (!string.IsNullOrEmpty(model.SecurityQuestion1))
            {
                questionNumber = 1;
                answerToCheck = model.SecurityAnswer1;
                
                if (string.IsNullOrEmpty(answerToCheck))
                {
                    ModelState.AddModelError("SecurityAnswer1", "Please provide an answer to the security question");
                    ViewData["ShowSecurityQuestionForm"] = true;
                    ViewData["SecurityQuestionModel"] = model;
                    return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
                }
            }
            else if (!string.IsNullOrEmpty(model.SecurityQuestion2))
            {
                questionNumber = 2;
                answerToCheck = model.SecurityAnswer2;
                
                if (string.IsNullOrEmpty(answerToCheck))
                {
                    ModelState.AddModelError("SecurityAnswer2", "Please provide an answer to the security question");
                    ViewData["ShowSecurityQuestionForm"] = true;
                    ViewData["SecurityQuestionModel"] = model;
                    return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
                }
            }
            else if (!string.IsNullOrEmpty(model.SecurityQuestion3))
            {
                questionNumber = 3;
                answerToCheck = model.SecurityAnswer3;
                
                if (string.IsNullOrEmpty(answerToCheck))
                {
                    ModelState.AddModelError("SecurityAnswer3", "Please provide an answer to the security question");
                    ViewData["ShowSecurityQuestionForm"] = true;
                    ViewData["SecurityQuestionModel"] = model;
                    return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
                }
            }
            else
            {
                // Get user ID from username
                var user = await _userRepository.GetUserByUsername(model.Username);
                
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found or security questions not set up for this account");
                    ViewData["ShowSecurityQuestionForm"] = true;
                    ViewData["SecurityQuestionModel"] = model;
                    return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
                }
                
                // Get security questions
                var questions = await _userRepository.GetSecurityQuestions(model.Username);
                
                if (string.IsNullOrEmpty(questions.Question1) && string.IsNullOrEmpty(questions.Question2) && 
                    string.IsNullOrEmpty(questions.Question3))
                {
                    ModelState.AddModelError(string.Empty, "Security questions not found for this account");
                    ViewData["ShowSecurityQuestionForm"] = true;
                    ViewData["SecurityQuestionModel"] = model;
                    return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
                }
                
                // Randomly select one question
                var random = new Random();
                int questionIndex = random.Next(1, 4);
                
                // Create new model with only selected question
                var newModel = new Project3.Shared.Models.InputModels.SecurityQuestionResetModel
                {
                    Username = model.Username
                };
                
                // Set only the selected question
                switch (questionIndex)
                {
                    case 1:
                        if (!string.IsNullOrEmpty(questions.Question1))
                        {
                            newModel.SecurityQuestion1 = questions.Question1;
                        }
                        else if (!string.IsNullOrEmpty(questions.Question2))
                        {
                            newModel.SecurityQuestion2 = questions.Question2;
                        }
                        else
                        {
                            newModel.SecurityQuestion3 = questions.Question3;
                        }
                        break;
                    case 2:
                        if (!string.IsNullOrEmpty(questions.Question2))
                        {
                            newModel.SecurityQuestion2 = questions.Question2;
                        }
                        else if (!string.IsNullOrEmpty(questions.Question3))
                        {
                            newModel.SecurityQuestion3 = questions.Question3;
                        }
                        else
                        {
                            newModel.SecurityQuestion1 = questions.Question1;
                        }
                        break;
                    case 3:
                        if (!string.IsNullOrEmpty(questions.Question3))
                        {
                            newModel.SecurityQuestion3 = questions.Question3;
                        }
                        else if (!string.IsNullOrEmpty(questions.Question1))
                        {
                            newModel.SecurityQuestion1 = questions.Question1;
                        }
                        else
                        {
                            newModel.SecurityQuestion2 = questions.Question2;
                        }
                        break;
                }
                
                ViewData["ShowSecurityQuestionForm"] = true;
                ViewData["SecurityQuestionModel"] = newModel;
                return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
            }
            
            // We are processing an answer verification
            // Get user ID from username
            var userData = await _userRepository.GetUserByUsername(model.Username);
            
            if (userData == null)
            {
                ModelState.AddModelError(string.Empty, "User not found. Please try again.");
                ViewData["ShowSecurityQuestionForm"] = true;
                ViewData["SecurityQuestionModel"] = model;
                return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
            }
            
            // Verify the answer
            bool isCorrect = await _authService.VerifySecurityAnswerAsync(userData.UserId, questionNumber, answerToCheck);
            
            if (isCorrect)
            {
                // Generate a token and redirect to the reset password page
                return RedirectToAction(nameof(ResetPassword), new { userId = userData.UserId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "The answer is incorrect. Please try again.");
                ViewData["ShowSecurityQuestionForm"] = true;
                ViewData["SecurityQuestionModel"] = model;
                return View(nameof(Login), new Project3.Shared.Models.ViewModels.LoginViewModel());
            }
        }

        [HttpGet("ForgotUsername")]
        [AllowAnonymous]
        public IActionResult ForgotUsername()
        {
            return View();
        }

        [HttpPost("ForgotUsername")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotUsername(ForgotUsernameModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Attempt to find user by email
                    var user = await _userRepository.GetUserByEmail(model.Email);
                    
                    if (user != null)
                    {
                        // Display the username directly
                        TempData["Message"] = $"Your username is: {user.Username}";
                    }
                    else
                    {
                        // For security reasons, don't reveal that no account exists
                        TempData["Message"] = "If an account exists with that email, the username has been sent.";
                    }
                    
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during username recovery process for {Email}", model.Email);
                    ModelState.AddModelError("", "An error occurred. Please try again.");
                }
            }
            
            return View(model);
        }
    }
} 