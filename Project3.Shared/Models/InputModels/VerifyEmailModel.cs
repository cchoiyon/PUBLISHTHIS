using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.InputModels
{
    public class VerifyEmailModel
    {
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; }
    }
} 