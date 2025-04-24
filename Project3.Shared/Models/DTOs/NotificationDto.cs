namespace Project3.Shared.Models.DTOs
{
    // DTO for notifications
    public class NotificationDto
    {
        public string Type { get; set; } // "Reservation" or "Review"
        public string Action { get; set; } // "Created", "Updated", "Deleted"
        public int Id { get; set; } // ReservationID or ReviewID
        public int RestaurantId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; }
        public object Data { get; set; } // Optional additional data
    }
} 
