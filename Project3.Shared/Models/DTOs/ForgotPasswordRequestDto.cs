
using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.DTOs
{
    // DTO for forgot password requests
    public class ForgotPasswordRequestDto
    {
        [Required]
        public string EmailOrUsername { get; set; }
    }
}

