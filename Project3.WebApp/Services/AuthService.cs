using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project3.WebApp.Models;
using Project3.WebApp.Repositories;
using System.Security.Claims;

namespace Project3.WebApp.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserRepository userRepository,
            EmailService emailService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        // Validate user credentials
        public async Task<UserData> ValidateCredentials(string username, string password)
        {
            try
            {
                var user = await _userRepository.GetUserByUsername(username);
                
                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for user: {Username}", username);
            }
            
            return null;
        }

        // Sign in user
        public async Task SignInAsync(HttpContext httpContext, UserData user, bool isPersistent = false)
        {
            var claimsIdentity = user.CreateIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent
            };
            
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
                
            _logger.LogInformation("User {Username} signed in", user.Username);
        }

        // Generate and send 2FA code
        public async Task<bool> Generate2FACodeAsync(UserData user)
        {
            try
            {
                string twoFactorCode = GenerateRandomCode();
                
                // Store code in database
                bool success = await _userRepository.SetTwoFactorCode(user.UserId, twoFactorCode);
                
                if (success)
                {
                    // Send email
                    await Send2FAEmail(user.Email, user.Username, twoFactorCode);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating 2FA code for user: {Username}", user.Username);
            }
            
            return false;
        }

        // Verify 2FA code
        public async Task<bool> Verify2FACodeAsync(int userId, string code)
        {
            try
            {
                return await _userRepository.VerifyTwoFactorCode(userId, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA code for user ID: {UserId}", userId);
                return false;
            }
        }

        // Clear 2FA token and mark user as verified
        public async Task<bool> ConfirmVerificationAsync(int userId)
        {
            try
            {
                bool clearSuccess = await _userRepository.ClearTwoFactorToken(userId);
                bool verifySuccess = await _userRepository.MarkUserAsVerified(userId);
                
                return clearSuccess && verifySuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming verification for user ID: {UserId}", userId);
                return false;
            }
        }

        // Generate a password reset token and send email
        public async Task<bool> GeneratePasswordResetAsync(string emailOrUsername)
        {
            try
            {
                var user = await _userRepository.GetUserByUsername(emailOrUsername);
                
                if (user == null)
                {
                    // Try to find by email instead
                    // (This would need an additional method in UserRepository)
                    return false;
                }
                
                string token = Guid.NewGuid().ToString("N");
                bool success = await _userRepository.SetPasswordResetToken(user.UserId, token);
                
                if (success)
                {
                    await SendPasswordResetEmail(user.Email, user.Username, user.UserId, token);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset for: {EmailOrUsername}", emailOrUsername);
            }
            
            return false;
        }

        // Reset password
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                return await _userRepository.ResetPassword(userId, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user ID: {UserId}", userId);
                return false;
            }
        }

        // Register a new user
        public async Task<(bool Success, int UserId, string ErrorMessage)> RegisterUserAsync(
            string username, string email, string password, string userRole,
            string question1, string answer1, string question2, string answer2, string question3, string answer3)
        {
            try
            {
                // Check if username/email is unique
                bool isUnique = await _userRepository.CheckUnique(username, email);
                
                if (!isUnique)
                {
                    return (false, 0, "Username or email already exists");
                }
                
                // Normalize role name
                string normalizedRole = NormalizeRoleName(userRole);
                
                // Hash password and security answers
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                string answer1Hash = BCrypt.Net.BCrypt.HashPassword(answer1);
                string answer2Hash = BCrypt.Net.BCrypt.HashPassword(answer2);
                string answer3Hash = BCrypt.Net.BCrypt.HashPassword(answer3);
                
                // Create user
                int userId = await _userRepository.CreateUser(
                    username, email, passwordHash, normalizedRole,
                    question1, answer1Hash, question2, answer2Hash, question3, answer3Hash);
                
                if (userId > 0)
                {
                    // Generate and send 2FA code
                    var user = new UserData
                    {
                        UserId = userId,
                        Username = username,
                        Email = email,
                        UserType = normalizedRole,
                        IsVerified = false
                    };
                    
                    await Generate2FACodeAsync(user);
                    
                    return (true, userId, null);
                }
                
                return (false, 0, "Failed to create user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Username}", username);
                return (false, 0, $"An error occurred: {ex.Message}");
            }
        }

        // Verify security question answer
        public async Task<bool> VerifySecurityAnswerAsync(int userId, int questionNumber, string answer)
        {
            try
            {
                string storedHash = await _userRepository.GetSecurityAnswerHash(userId, questionNumber);
                
                if (!string.IsNullOrEmpty(storedHash))
                {
                    return BCrypt.Net.BCrypt.Verify(answer, storedHash);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying security answer for user ID: {UserId}, question: {Question}", 
                    userId, questionNumber);
            }
            
            return false;
        }

        // Helper methods
        private string GenerateRandomCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private string NormalizeRoleName(string role)
        {
            if (string.Equals(role, "reviewer", StringComparison.OrdinalIgnoreCase))
            {
                return "Reviewer";
            }
            else if (string.Equals(role, "restaurantrep", StringComparison.OrdinalIgnoreCase))
            {
                return "RestaurantRep";
            }
            
            return role;
        }

        // Email sending methods
        private async Task Send2FAEmail(string email, string username, string code)
        {
            string subject = "Yelp 2.0 - Your Two-Factor Authentication Code";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Two-Factor Authentication Code</h2>
                    <p>Hello {username},</p>
                    <p>Your verification code is:</p>
                    <h3 style='background-color: #f5f5f5; padding: 10px; text-align: center;'>{code}</h3>
                    <p>This code will expire in 15 minutes.</p>
                    <p>If you did not request this code, please secure your account immediately.</p>
                    <p>Regards,<br/>Yelp 2.0 Team</p>
                </body>
                </html>";
            
            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendPasswordResetEmail(string email, string username, int userId, string token)
        {
            // Get the web app URL based on environment
            var webAppUrl = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                ? "https://localhost:7167"
                : "https://cis-iis2.temple.edu/Spring2025/CIS3342_tuo53004/termproject";
                
            string subject = "Yelp 2.0 - Password Reset Request";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Request</h2>
                    <p>Hello {username},</p>
                    <p>We received a request to reset your password. Please click the link below to reset it:</p>
                    <p><a href='{webAppUrl}/Account/ResetPassword?userId={userId}&token={token}'>Reset Your Password</a></p>
                    <p>This link will expire in 2 hours.</p>
                    <p>If you didn't request a password reset, please ignore this email.</p>
                    <p>Regards,<br/>Yelp 2.0 Team</p>
                </body>
                </html>";
            
            await _emailService.SendEmailAsync(email, subject, body);
        }
    }
} 