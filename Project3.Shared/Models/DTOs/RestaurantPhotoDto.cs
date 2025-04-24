using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.DTOs
{
    // Consolidated DTO for restaurant photo operations
    public class RestaurantPhotoDto
    {
        // Used in responses and for identifying existing photos
        public int? PhotoID { get; set; }
        
        // Used when creating new photos
        [Required(ErrorMessage = "Photo URL is required")]
        public string PhotoURL { get; set; }
        
        // Used for both adding and updating captions
        [StringLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
        public string Caption { get; set; }
        
        // Used in responses
        public DateTime? UploadedDate { get; set; }
        
        // For associating with a restaurant
        public int RestaurantID { get; set; }
    }
} 