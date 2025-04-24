using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Project3.Shared.Models.Configuration;
using Project3.Shared.Models.ViewModels;
using Project3.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;

namespace Project3.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountApiController : ControllerBase
    {
        private readonly ILogger<AccountApiController> _logger;
        private readonly Connection _dbConnect;
        private readonly Email _emailService;
        private readonly SmtpSettings _smtpSettings;
        private readonly IConfiguration _configuration;

        public record LoginResponseDto(bool IsAuthenticated, bool IsVerified, int UserId, string Username, string Email, string Role);
        public record ErrorResponseDto(string Message);
        public record VerificationRequestDto(string VerificationToken);
        public record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);
        public record ForgotPasswordRequestDto(string EmailOrUsername);
        public record RegisterRequestDto(
                 string Username, string Email, string Password, string UserRole, string FirstName, string LastName,
                 string SecurityQuestion1, string SecurityAnswerHash1,
                 string SecurityQuestion2, string SecurityAnswerHash2,
                 string SecurityQuestion3, string SecurityAnswerHash3
        );

        public AccountApiController(
            ILogger<AccountApiController> logger,
            Connection dbConnect,
            Email emailService,
            IOptions<SmtpSettings> smtpSettingsOptions,
            IConfiguration configuration)
        {
            _logger = logger;
            _dbConnect = dbConnect;
            _emailService = emailService;
            _smtpSettings = smtpSettingsOptions.Value;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginViewModel loginModel)
        {
            _logger.LogInformation("API: Login attempt for username {Username}", loginModel.Username);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            DataSet ds = null;

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spCheckUser");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", loginModel.Username);
                cmd.Parameters.AddWithValue("@UserPassword", loginModel.Password);

                ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];
                    string storedHash = dr["PasswordHash"]?.ToString();
                    bool isVerified = Convert.ToBoolean(dr["IsVerified"]);
                    string userRole = dr["UserType"]?.ToString() ?? "Guest";
                    int userId = Convert.ToInt32(dr["UserID"]);
                    string email = dr.Table.Columns.Contains("Email") ? dr["Email"]?.ToString() : string.Empty;

                    bool passwordIsValid = false;
                    if (!string.IsNullOrEmpty(storedHash))
                    {
                        try { passwordIsValid = BCrypt.Net.BCrypt.Verify(loginModel.Password, storedHash); }
                        catch (Exception hashEx) { _logger.LogError(hashEx, "BCrypt verification failed during login for {Username} with exception.", loginModel.Username); }
                    }
                    else { _logger.LogWarning("API: Password hash not found in DB for user {Username}", loginModel.Username); }

                    if (!passwordIsValid)
                    {
                        _logger.LogWarning("API: BCrypt.Verify returned false for user {Username}. Password check failed.", loginModel.Username);
                        _logger.LogWarning("API: Invalid password provided for user {Username}", loginModel.Username);
                        return Unauthorized(new ErrorResponseDto("Invalid username or password."));
                    }

                    var responseDto = new LoginResponseDto(true, isVerified, userId, loginModel.Username, email, userRole);
                    _logger.LogInformation("API: Login successful for user {Username}", loginModel.Username);
                    return Ok(responseDto);
                }
                else
                {
                    _logger.LogWarning("API: User not found for username {Username} (DB query returned no rows).", loginModel.Username);
                    return Unauthorized(new ErrorResponseDto("Invalid username or password."));
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error during login for {Username}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    loginModel.Username, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal database error occured during login. Please check logs."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error during login for {Username}", loginModel.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal error occured during login."));
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registrationData)
        {
            _logger.LogInformation("API: Registration attempt for username {Username}", registrationData.Username);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                string verificationCode = GenerateSecureToken();
                DateTime expiryTime = DateTime.UtcNow.AddHours(24);
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registrationData.Password);

                SqlCommand cmd = new SqlCommand("dbo.TP_spAddUser");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Username", registrationData.Username);
                cmd.Parameters.AddWithValue("@Email", registrationData.Email);
                cmd.Parameters.AddWithValue("@UserPassword", hashedPassword);
                cmd.Parameters.AddWithValue("@UserType", registrationData.UserRole);
                cmd.Parameters.AddWithValue("@SecurityQuestion1", registrationData.SecurityQuestion1);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash1", registrationData.SecurityAnswerHash1);
                cmd.Parameters.AddWithValue("@SecurityQuestion2", registrationData.SecurityQuestion2);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash2", registrationData.SecurityAnswerHash2);
                cmd.Parameters.AddWithValue("@SecurityQuestion3", registrationData.SecurityQuestion3);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash3", registrationData.SecurityAnswerHash3);
                cmd.Parameters.AddWithValue("@VerificationToken", verificationCode);
                cmd.Parameters.AddWithValue("@VerificationTokenExpiry", expiryTime);

                object result = _dbConnect.ExecuteScalarFunction(cmd);

                int registeredUserId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                if (registeredUserId > 0)
                {
                    _logger.LogInformation("API: User {Username} registered with ID {UserId}. Sending verification email.", registrationData.Username, registeredUserId);
                    await SendConfirmationEmail(registeredUserId, registrationData.Email, verificationCode);
                    return Ok(new { Message = "Registration successfull. Please check your email to verify your account." });
                }
                else
                {
                    _logger.LogError("API: User registration failed for {Username} (ExecuteScalar returned null/0 or DB error).", registrationData.Username);
                    return BadRequest(new ErrorResponseDto("Registration failed. Could not create user record."));
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error during registration for {Username}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    registrationData.Username, sqlEx.Number, sqlEx.Message);
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    return BadRequest(new ErrorResponseDto("Registration failed. Username or email may already exist."));
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal database eror occured during registration. Please check logs."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error during registration for {Username}", registrationData.Username);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An internal error occured during registration."));
            }
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] VerificationRequestDto verificationData)
        {
            if (string.IsNullOrEmpty(verificationData.VerificationToken))
            {
                return BadRequest(new ErrorResponseDto("Verification token is required."));
            }

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spVerifyUser");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@VerificationToken", verificationData.VerificationToken);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: User account verified with token {Token}", verificationData.VerificationToken);
                    return Ok(new { Message = "Email verification successfull. Your account is now active." });
                }
                else
                {
                    _logger.LogWarning("API: Verification failed - Token not found or expired: {Token}", verificationData.VerificationToken);
                    return BadRequest(new ErrorResponseDto("Verification failed. The token may be invalid or expired."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error verifying account with token {Token}", verificationData.VerificationToken);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An error occured while verifying your account."));
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto forgotPasswordData)
        {
            if (string.IsNullOrEmpty(forgotPasswordData.EmailOrUsername))
            {
                return BadRequest(new ErrorResponseDto("Email or username is required."));
            }

            try
            {
                string resetToken = GenerateSecureToken();
                DateTime expiryTime = DateTime.UtcNow.AddHours(1);

                SqlCommand findUserCmd = new SqlCommand("dbo.TP_spFindUserByEmailOrUsername");
                findUserCmd.CommandType = CommandType.StoredProcedure;
                findUserCmd.Parameters.AddWithValue("@EmailOrUsername", forgotPasswordData.EmailOrUsername);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(findUserCmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow dr = ds.Tables[0].Rows[0];
                    int userId = Convert.ToInt32(dr["UserID"]);
                    string email = dr["Email"]?.ToString();
                    string username = dr["Username"]?.ToString();

                    if (string.IsNullOrEmpty(email))
                    {
                        _logger.LogWarning("API: ForgotPassword failed - User {Username} has no email address", username);
                        return BadRequest(new ErrorResponseDto("This account does not have an email address on file."));
                    }

                    SqlCommand updateCmd = new SqlCommand("dbo.TP_spSetPasswordResetToken");
                    updateCmd.CommandType = CommandType.StoredProcedure;
                    updateCmd.Parameters.AddWithValue("@UserID", userId);
                    updateCmd.Parameters.AddWithValue("@ResetToken", resetToken);
                    updateCmd.Parameters.AddWithValue("@ResetTokenExpiry", expiryTime);

                    int updateResult = _dbConnect.DoUpdateUsingCmdObj(updateCmd);

                    if (updateResult > 0)
                    {
                        // Get the appropriate web app URL based on environment
                        var webAppUrl = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                            ? _configuration["ApplicationUrls:WebApp"]
                            : _configuration["Production:WebApp"];
                            
                        string resetLink = $"{webAppUrl}/Account/ResetPassword?userId={userId}&token={WebUtility.UrlEncode(resetToken)}";
                        string emailBody = $@"
                            <h2>Password Reset Request</h2>
                            <p>We received a request to reset your password. Please click the link below to reset it:</p>
                            <p><a href='{resetLink}'>Reset Your Password</a></p>
                            <p>This link will expire in 1 hour.</p>
                            <p>If you didn't request a password reset, please ignore this email.</p>
                            <p>Regards,<br/>Yelp 2.0 Team</p>";

                        await Task.Run(() => _emailService.SendMail(email, "tuo53004@temple.edu", "Yelp 2.0 - Password Reset Request", emailBody));

                        _logger.LogInformation("API: Password reset token generated for User {Username}, token expires {ExpiryTime}", username, expiryTime);
                        return Ok(new { Message = "Reset instructions have been sent to your email adress." });
                    }
                    else
                    {
                        _logger.LogError("API: Could not update reset token for User {UserId}", userId);
                        return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Could not process your request. Please try again later."));
                    }
                }
                else
                {
                    _logger.LogWarning("API: ForgotPassword requested for non-existent user {EmailOrUsername}", forgotPasswordData.EmailOrUsername);
                    return Ok(new { Message = "If your email is registered with us, you will receive reset instructions." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error processing forgot password request for {EmailOrUsername}", forgotPasswordData.EmailOrUsername);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An error occured while processing your request."));
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto resetPasswordData)
        {
            if (string.IsNullOrEmpty(resetPasswordData.Token) || string.IsNullOrEmpty(resetPasswordData.NewPassword) || string.IsNullOrEmpty(resetPasswordData.UserId))
            {
                return BadRequest(new ErrorResponseDto("User ID, token, and new password are required."));
            }

            try
            {
                if (!int.TryParse(resetPasswordData.UserId, out int userId))
                {
                    return BadRequest(new ErrorResponseDto("Invalid user ID format."));
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(resetPasswordData.NewPassword);

                SqlCommand cmd = new SqlCommand("dbo.TP_spResetPassword");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ResetToken", resetPasswordData.Token);
                cmd.Parameters.AddWithValue("@NewPasswordHash", hashedPassword);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Password reset successful for User ID {UserId}", userId);
                    return Ok(new { Message = "Your password has been reset successfuly. You can now login with your new password." });
                }
                else
                {
                    _logger.LogWarning("API: Password reset failed for User ID {UserId} - Invalid or expired token", userId);
                    return BadRequest(new ErrorResponseDto("Password reset failed. The token may be invalid or expired."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error resetting password for User ID {UserId}", resetPasswordData.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("An error occured while resetting your password."));
            }
        }

        private async Task SendConfirmationEmail(int userId, string userEmail, string verificationCode)
        {
            if (string.IsNullOrEmpty(userEmail)) return;

            try
            {
                // Get the appropriate web app URL based on environment
                var webAppUrl = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                    ? _configuration["ApplicationUrls:WebApp"]
                    : _configuration["Production:WebApp"];
                    
                string confirmationLink = $"{webAppUrl}/Account/Verify?token={WebUtility.UrlEncode(verificationCode)}";
                string emailBody = $@"
                    <h2>Confirm Your Email Address</h2>
                    <p>Thank you for signing up! Please click the link below to confirm your email address:</p>
                    <p><a href='{confirmationLink}'>Confirm Your Email</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't register for this account, please ignore this email.</p>
                    <p>Regards,<br/>Yelp 2.0 Team</p>";

                await Task.Run(() => _emailService.SendMail(userEmail, "tuo53004@temple.edu", "Yelp 2.0 - Account Verification", emailBody));
                
                _logger.LogInformation("API: Verification email sent to {UserEmail} for User ID {UserId}", userEmail, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Failed to send verification email to {UserEmail} for User ID {UserId}", userEmail, userId);
            }
        }

        private string GenerateSecureToken()
        {
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
    }
} 