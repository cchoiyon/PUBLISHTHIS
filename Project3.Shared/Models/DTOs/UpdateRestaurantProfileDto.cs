using System.ComponentModel.DataAnnotations; // Required for validation attributes
namespace Project3.Shared.Models.DTOs
{
    // DTO for data transfer
    public class UpdateRestaurantProfileDto
    {
        // DTO for data transfer
        [Required(ErrorMessage = "Restaurant ID is required to update a profile.")]
        public int RestaurantID { get; set; }

        // DTO for data transfer
        [Required(ErrorMessage = "Restaurant Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; }

        // DTO for data transfer
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string? Address { get; set; } // Nullable string

        // DTO for data transfer
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; } // Added, Nullable string

        // DTO for data transfer
        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
        public string? State { get; set; } // Added, Nullable string

        // DTO for data transfer
        [StringLength(10, ErrorMessage = "Zip Code cannot exceed 10 characters.")]
        public string? ZipCode { get; set; } // Added, Nullable string

        // DTO for data transfer
        [StringLength(100, ErrorMessage = "Cuisine type cannot exceed 100 characters.")]
        public string? Cuisine { get; set; } // Added (replaces 'Type'), Nullable string

        // DTO for data transfer
        [StringLength(255, ErrorMessage = "Hours description cannot exceed 255 characters.")]
        public string? Hours { get; set; } // Added, Nullable string

        // DTO for data transfer
        [StringLength(100, ErrorMessage = "Contact information cannot exceed 100 characters.")]
        public string? Contact { get; set; } // Added (replaces 'Phone'), Nullable string

        // DTO for data transfer
        [DataType(DataType.MultilineText)] // Suggests a larger text input area in UI
        public string? MarketingDescription { get; set; } // Added (replaces 'Description'), Nullable string

        // DTO for data transfer
        [Url(ErrorMessage = "Please enter a valid Website URL (e.g., http://www.example.com).")]
        [StringLength(255, ErrorMessage = "Website URL cannot exceed 255 characters.")]
        public string? WebsiteURL { get; set; } // Nullable string

        // DTO for data transfer
        public string? SocialMedia { get; set; } // Added, Nullable string

        // DTO for data transfer
        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters.")]
        public string? Owner { get; set; } // Added, Nullable string

        // DTO for data transfer
        public string? ProfilePhoto { get; set; } // Kept as nullable string

        // DTO for data transfer
        public string? LogoPhoto { get; set; } // Kept as nullable string
        public UpdateRestaurantProfileDto()
        {
            Name = string.Empty; // Initialize to avoid null warnings if not set immediately
        }
    }
}

