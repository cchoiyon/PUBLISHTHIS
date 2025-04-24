using System.ComponentModel.DataAnnotations;

// DTO for updating an existing review
namespace Project3.Shared.Models.DTOs
{
    // DTO for data transfer
    public class UpdateReviewDto
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }

        [Required]
        [Range(1, 5)]
        public int FoodQualityRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int ServiceRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int AtmosphereRating { get; set; }

        [Required]
        [Range(1, 5)]
        public int PriceRating { get; set; }
    }
}

