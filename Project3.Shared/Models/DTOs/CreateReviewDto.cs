using System.ComponentModel.DataAnnotations;


namespace Project3.Shared.Models.DTOs
{
    // DTO for data transfer
    public class CreateReviewDto
    {
        [Required]
        public int RestaurantID { get; set; }

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

