using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.DTOs
{
    // DTO for password reset requests
    public class ResetPasswordRequestDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }
}

