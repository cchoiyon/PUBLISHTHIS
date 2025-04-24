

namespace Project3.Shared.Models.InputModels
{

    [Serializable]
    public class ForgotUsernameModel
    {
        // Private backing field
        private string _email;

        // Public property with explicit get/set
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        // Parameterless constructor (matches requested style)
        public ForgotUsernameModel() { }

        // Optional: Parameterized constructor (matches requested style)
        public ForgotUsernameModel(string email)
        {
            _email = email;
        }
    }
}
