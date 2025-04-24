using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.DTOs
{
    // DTO for creating a new reservation
    public class CreateReservationDto
    {
        [Required]
        public int RestaurantID { get; set; }
        
        [Required]
        public DateTime ReservationDateTime { get; set; }
        
        [Required]
        [Range(1, 20, ErrorMessage = "Party size must be between 1 and 20")]
        public int PartySize { get; set; }
        
        [Required]
        [StringLength(100, ErrorMessage = "Contact name cannot exceed 100 characters")]
        public string ContactName { get; set; }
        
        [Phone]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string Phone { get; set; }
        
        [EmailAddress]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }
        
        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        public string SpecialRequests { get; set; }
    }

    // DTO for updating reservation status
    public class UpdateStatusDto
    {
        [Required]
        [RegularExpression("^(Pending|Confirmed|Cancelled|Completed|NoShow)$", 
            ErrorMessage = "Status must be one of: Pending, Confirmed, Cancelled, Completed, NoShow")]
        public string Status { get; set; }
    }

    // DTO for reservation response
    public class ReservationResponseDto
    {
        public int ReservationID { get; set; }
        public int RestaurantID { get; set; }
        public string RestaurantName { get; set; }
        public int? UserID { get; set; }
        public string Username { get; set; }
        public DateTime ReservationDateTime { get; set; }
        public int PartySize { get; set; }
        public string ContactName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string SpecialRequests { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
} 
