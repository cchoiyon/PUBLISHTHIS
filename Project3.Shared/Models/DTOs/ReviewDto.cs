namespace Project3.Shared.Models.DTOs
{
    // DTO for data transfer
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public string RestaurantName { get; set; }
        public decimal Rating { get; set; }
        public string Comment { get; set; }
        public DateTime ReviewDate { get; set; }
        public string ReviewerUsername { get; set; }
        
        // Default constructor
        public ReviewDto()
        {
            // Initialize if needed
        }
    }
}

