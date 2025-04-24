
using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.InputModels
{
    public class RegisterModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Account Type")]
        public string UserRole { get; set; }

        [Required]
        [Display(Name = "Security Question 1")]
        public string SecurityQuestion1 { get; set; }

        [Required]
        [Display(Name = "Security Answer 1")]
        public string SecurityAnswer1 { get; set; }

        [Required]
        [Display(Name = "Security Question 2")]
        public string SecurityQuestion2 { get; set; }

        [Required]
        [Display(Name = "Security Answer 2")]
        public string SecurityAnswer2 { get; set; }

        [Required]
        [Display(Name = "Security Question 3")]
        public string SecurityQuestion3 { get; set; }

        [Required]
        [Display(Name = "Security Answer 3")]
        public string SecurityAnswer3 { get; set; }
    }
}
