using System;

namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class User
    {
        // Backing fields
        private int _userID;
        private string _username;
        private string _passwordHash;
        private string _email;
        private string _userType;

        // Properties
        public int UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = value; }
        }

        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        public string UserType
        {
            get { return _userType; }
            set { _userType = value; }
        }

        // Default constructor
        public User()
        {
        }

        // Constructor with parameters
        public User(string username, string passwordHash, string email, string userType)
        {
            _username = username;
            _passwordHash = passwordHash;
            _email = email;
            _userType = userType;
        }
    }
}