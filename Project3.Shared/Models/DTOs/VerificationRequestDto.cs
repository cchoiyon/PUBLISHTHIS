using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.DTOs
{
    // DTO for email verification
    public class VerificationRequestDto
    {
        [Required]
        public string VerificationToken { get; set; }
    }
}

