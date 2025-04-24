using Project3.Shared.Utilities;
using Project3.WebApp.Models;
using System.Data;
using System.Data.SqlClient;

namespace Project3.WebApp.Repositories
{
    // This repository is super important for simplifying our controller code!
    // Got this idea from "ASP.NET Core MVC in Action" book - repository pattern
    // helps keep SQL queries out of controllers and makes everything more maintainable
    public class UserRepository
    {
        private readonly Connection _db;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(Connection db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        // gets user by username - basic but super useful
        public async Task<UserData> GetUserByUsername(string username)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("SELECT UserID, Username, PasswordHash, UserType, Email, ISNULL(IsVerified, 0) AS IsVerified FROM TP_Users WHERE Username = @Username");
                cmd.Parameters.AddWithValue("@Username", username);
                
                var ds = _db.GetDataSetUsingCmdObj(cmd);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    
                    return new UserData
                    {
                        UserId = Convert.ToInt32(row["UserID"]),
                        Username = row["Username"].ToString(),
                        Email = row["Email"].ToString(),
                        PasswordHash = row["PasswordHash"].ToString(),
                        UserType = row["UserType"].ToString(),
                        IsVerified = Convert.ToBoolean(row["IsVerified"])
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by username: {Username}", username);
            }
            
            return null;
        }

        // finds user by ID - used this tons in AccountController
        // finally pulled it out to make controller way simpler
        public async Task<UserData> GetUserById(int userId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("SELECT UserID, Username, PasswordHash, UserType, Email, ISNULL(IsVerified, 0) AS IsVerified FROM TP_Users WHERE UserID = @UserID");
                cmd.Parameters.AddWithValue("@UserID", userId);
                
                var ds = _db.GetDataSetUsingCmdObj(cmd);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    
                    return new UserData
                    {
                        UserId = Convert.ToInt32(row["UserID"]),
                        Username = row["Username"].ToString(),
                        Email = row["Email"].ToString(),
                        PasswordHash = row["PasswordHash"].ToString(),
                        UserType = row["UserType"].ToString(),
                        IsVerified = Convert.ToBoolean(row["IsVerified"])
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
            }
            
            return null;
        }

        // 2FA stuff - moved from controller to make it way cleaner
        // book said "keep controllers skinny" - this helps a ton!
        public async Task<bool> SetTwoFactorCode(int userId, string code, int expiryMinutes = 15)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(
                    "UPDATE TP_Users SET TwoFactorToken = @TwoFactorToken, TwoFactorTokenExpiry = DATEADD(MINUTE, @ExpiryMinutes, GETDATE()) " +
                    "WHERE UserID = @UserID");
                cmd.Parameters.AddWithValue("@TwoFactorToken", code);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ExpiryMinutes", expiryMinutes);
                
                int rowsAffected = _db.DoUpdateUsingCmdObj(cmd);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting 2FA code for user: {UserId}", userId);
                return false;
            }
        }

        // this checks if 2FA code is valid AND not expired - nice!
        public async Task<bool> VerifyTwoFactorCode(int userId, string code)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM TP_Users WHERE UserID = @UserID AND TwoFactorToken = @TwoFactorToken " +
                    "AND TwoFactorTokenExpiry > GETDATE()");
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@TwoFactorToken", code);
                
                int matches = Convert.ToInt32(_db.ExecuteScalarUsingCmdObj(cmd));
                return matches > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA code for user: {UserId}", userId);
                return false;
            }
        }

        // marks a user as verified - so much cleaner than having this in controller!
        public async Task<bool> MarkUserAsVerified(int userId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("UPDATE TP_Users SET IsVerified = 1 WHERE UserID = @UserID");
                cmd.Parameters.AddWithValue("@UserID", userId);
                
                int rowsAffected = _db.DoUpdateUsingCmdObj(cmd);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking user as verified: {UserId}", userId);
                return false;
            }
        }

        // using stored proc here - so much better than inline SQL everywhere
        // learned this from book examples - keeps DB logic in DB where it belongs!
        public async Task<bool> ClearTwoFactorToken(int userId)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("TP_spClear2FATokenAndVerify");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userId);
                
                int rowsAffected = _db.DoUpdateUsingCmdObj(cmd);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing 2FA token for user: {UserId}", userId);
                return false;
            }
        }

        // password reset token stuff - was a mess before repository pattern!
        public async Task<bool> SetPasswordResetToken(int userId, string token, int expiryHours = 2)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("TP_spSetPasswordResetToken");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ResetToken", token);
                cmd.Parameters.AddWithValue("@ExpiryHours", expiryHours);
                
                int rowsAffected = _db.DoUpdateUsingCmdObj(cmd);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting password reset token for user: {UserId}", userId);
                return false;
            }
        }

        // password reset - so much cleaner in repository vs controller
        public async Task<bool> ResetPassword(int userId, string newPasswordHash)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(@"
                    UPDATE TP_Users 
                    SET PasswordHash = @PasswordHash, 
                        PasswordResetToken = NULL, 
                        ResetTokenExpiry = NULL 
                    WHERE UserID = @UserID");
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@PasswordHash", newPasswordHash);
                
                int rowsAffected = _db.DoUpdateUsingCmdObj(cmd);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
                return false;
            }
        }

        // user creation - uses stored proc but falls back if it fails
        // book mentioned this pattern for more robust code
        public async Task<int> CreateUser(string username, string email, string passwordHash, string userType, 
            string question1, string answerHash1, string question2, string answerHash2, string question3, string answerHash3)
        {
            try
            {
                // Use the stored procedure first
                SqlCommand cmd = new SqlCommand("TP_spAddUser");
                cmd.CommandType = CommandType.StoredProcedure;
                
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@UserType", userType);
                cmd.Parameters.AddWithValue("@SecurityQuestion1", question1);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash1", answerHash1);
                cmd.Parameters.AddWithValue("@SecurityQuestion2", question2);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash2", answerHash2);
                cmd.Parameters.AddWithValue("@SecurityQuestion3", question3);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash3", answerHash3);
                
                // Try stored procedure
                var result = _db.ExecuteScalarUsingCmdObj(cmd);
                
                if (result != null && int.TryParse(result.ToString(), out int userId))
                {
                    return userId;
                }
                
                // Fall back to direct SQL if stored procedure doesn't work
                cmd = new SqlCommand(@"
                    INSERT INTO TP_Users (
                        Username, Email, PasswordHash, UserType,
                        SecurityQuestion1, SecurityAnswerHash1,
                        SecurityQuestion2, SecurityAnswerHash2,
                        SecurityQuestion3, SecurityAnswerHash3,
                        IsVerified, CreatedDate
                    ) 
                    VALUES (
                        @Username, @Email, @PasswordHash, @UserType,
                        @SecurityQuestion1, @SecurityAnswerHash1,
                        @SecurityQuestion2, @SecurityAnswerHash2,
                        @SecurityQuestion3, @SecurityAnswerHash3,
                        0, GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();");
                    
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@UserType", userType);
                cmd.Parameters.AddWithValue("@SecurityQuestion1", question1);
                cmd.Parameters.AddWithValue("@SecurityQuestion2", question2);
                cmd.Parameters.AddWithValue("@SecurityQuestion3", question3);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash1", answerHash1);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash2", answerHash2);
                cmd.Parameters.AddWithValue("@SecurityAnswerHash3", answerHash3);
                
                result = _db.ExecuteScalarUsingCmdObj(cmd);
                
                if (result != null && int.TryParse(result.ToString(), out userId))
                {
                    return userId;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", username);
                return 0;
            }
        }

        // security question stuff for reset password - this was a mess before!
        // book said this is a perfect candidate for repository pattern
        public async Task<(string Question1, string Question2, string Question3)> GetSecurityQuestions(string username)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("SELECT SecurityQuestion1, SecurityQuestion2, SecurityQuestion3 FROM TP_Users WHERE Username = @Username");
                cmd.Parameters.AddWithValue("@Username", username);
                
                var ds = _db.GetDataSetUsingCmdObj(cmd);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    
                    return (
                        Question1: row["SecurityQuestion1"]?.ToString(),
                        Question2: row["SecurityQuestion2"]?.ToString(),
                        Question3: row["SecurityQuestion3"]?.ToString()
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security questions for user: {Username}", username);
            }
            
            return (null, null, null);
        }

        // gets security answer hash for verification - cleaner than controller code
        public async Task<string> GetSecurityAnswerHash(int userId, int questionNumber)
        {
            try
            {
                string columnName = $"SecurityAnswerHash{questionNumber}";
                SqlCommand cmd = new SqlCommand($"SELECT {columnName} FROM TP_Users WHERE UserID = @UserID");
                cmd.Parameters.AddWithValue("@UserID", userId);
                
                var result = _db.ExecuteScalarUsingCmdObj(cmd);
                return result?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security answer hash for user: {UserId}, question: {Question}", 
                    userId, questionNumber);
                return null;
            }
        }

        // checks if username/email combo is unique - moved from controller
        // made registration way cleaner - another win for repository pattern!
        public async Task<bool> CheckUnique(string username, string email)
        {
            try
            {
                // Check if username exists
                SqlCommand usernameCmd = new SqlCommand("SELECT COUNT(*) FROM TP_Users WHERE Username = @Username");
                usernameCmd.Parameters.AddWithValue("@Username", username);
                int usernameCount = Convert.ToInt32(_db.ExecuteScalarUsingCmdObj(usernameCmd));
                
                if (usernameCount > 0)
                {
                    return false;
                }
                
                // Check if email exists
                SqlCommand emailCmd = new SqlCommand("SELECT COUNT(*) FROM TP_Users WHERE Email = @Email");
                emailCmd.Parameters.AddWithValue("@Email", email);
                int emailCount = Convert.ToInt32(_db.ExecuteScalarUsingCmdObj(emailCmd));
                
                return emailCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking uniqueness for username: {Username}, email: {Email}", 
                    username, email);
                return false;
            }
        }

        // needed this for the forgot username feature - so handy!
        // book examples showed how valuable this kind of method is
        public async Task<UserData> GetUserByEmail(string email)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("SELECT UserID, Username, PasswordHash, UserType, Email, ISNULL(IsVerified, 0) AS IsVerified FROM TP_Users WHERE Email = @Email");
                cmd.Parameters.AddWithValue("@Email", email);
                
                var ds = _db.GetDataSetUsingCmdObj(cmd);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    
                    return new UserData
                    {
                        UserId = Convert.ToInt32(row["UserID"]),
                        Username = row["Username"].ToString(),
                        Email = row["Email"].ToString(),
                        PasswordHash = row["PasswordHash"].ToString(),
                        UserType = row["UserType"].ToString(),
                        IsVerified = Convert.ToBoolean(row["IsVerified"])
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            }
            
            return null;
        }
    }
} 