

namespace Project3.Shared.Models.InputModels // Updated namespace to match project structure
{
 
    [Serializable] // Added to match requested style
    public class ForgotPasswordModel
    {
        // Private backing field
        private string _emailOrUsername;

        // Public property with explicit get/set accessors
        public string EmailOrUsername
        {
            get { return _emailOrUsername; }
            set { _emailOrUsername = value; }
        }

        // Parameterless constructor (common in the requested style)
        public ForgotPasswordModel()
        {
            // Initialize if necessary, e.g.:
            // _emailOrUsername = string.Empty;
        }

        // Optional: Parameterized constructor (also seen in the requested style)
        public ForgotPasswordModel(string emailOrUsername)
        {
            _emailOrUsername = emailOrUsername;
        }
    }
}
