using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.DTOs
{
    // DTO for adding photos
    public class AddPhotoDto
    {
        [Required]
        public string PhotoURL { get; set; }

        [StringLength(500)]
        public string? Caption { get; set; }
    }
}

