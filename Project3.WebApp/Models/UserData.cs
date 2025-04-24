using System.Security.Claims;

namespace Project3.WebApp.Models
{
    public class UserData
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string UserType { get; set; }
        public bool IsVerified { get; set; }

        // Helper method to create claims identity
        public ClaimsIdentity CreateIdentity(string authenticationScheme)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Username),
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim(ClaimTypes.Role, NormalizeRole())
            };
            
            return new ClaimsIdentity(claims, authenticationScheme);
        }

        // Helper method to normalize role name
        private string NormalizeRole()
        {
            if (string.Equals(UserType, "reviewer", StringComparison.OrdinalIgnoreCase))
            {
                return "Reviewer";
            }
            else if (string.Equals(UserType, "restaurantrep", StringComparison.OrdinalIgnoreCase))
            {
                return "RestaurantRep";
            }
            
            return UserType ?? "User";
        }
    }
} 