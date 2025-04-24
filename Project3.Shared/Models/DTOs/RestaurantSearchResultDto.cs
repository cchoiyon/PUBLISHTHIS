namespace Project3.Shared.Models.DTOs
{
    // DTO for data transfer
    public class RestaurantSearchResultDto
    {
        public int RestaurantID { get; set; }
        public string Name { get; set; }
        public string? LogoPhoto { get; set; }
        public string? Cuisine { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public double OverallRating { get; set; }
        public int ReviewCount { get; set; }
        public double AveragePriceRating { get; set; }
    }
}

