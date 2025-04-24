

namespace Project3.Shared.Models.InputModels
{
   
    [Serializable]
    public class ResetPasswordModel
    {
        // Private backing fields
        private string _userId;
        private string _token;
        private string _newPassword;
        private string _confirmPassword;
        private bool _resetFromSecurityQuestion;

        // Public property for User ID
        public string UserId // Or int
        {
            get { return _userId; }
            set { _userId = value; }
        }

        // Public property for Token
        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }

        // Public property for New Password
        public string NewPassword
        {
            get { return _newPassword; }
            set { _newPassword = value; }
        }

        // Public property for Confirm Password
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; }
        }
        
        // Flag to indicate if this reset is from security question verification (no token needed)
        public bool ResetFromSecurityQuestion
        {
            get { return _resetFromSecurityQuestion; }
            set { _resetFromSecurityQuestion = value; }
        }

        // Parameterless constructor (matches requested style)
        public ResetPasswordModel() { }

        // Optional: Parameterized constructor (matches requested style)
        public ResetPasswordModel(string userId, string token, string newPassword, string confirmPassword, bool resetFromSecurityQuestion = false)
        {
            _userId = userId;
            _token = token;
            _newPassword = newPassword;
            _confirmPassword = confirmPassword;
            _resetFromSecurityQuestion = resetFromSecurityQuestion;
        }
    }
}
