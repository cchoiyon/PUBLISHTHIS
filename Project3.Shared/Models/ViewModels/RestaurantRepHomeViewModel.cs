// Using statements needed for the properties below
using Project3.Shared.Models.DTOs;
using System.Collections.Generic; // For List<>
// Removed using Project3.Models.Domain; as Restaurant and Reservation objects are no longer direct properties
// Removed using Project3.Models.InputModels; as ReviewViewModel is replaced by ReviewDto
namespace Project3.Shared.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Restaurant Rep Home page dashboard.
    /// Updated to use auto-properties and match the view's requirements.
    /// </summary>
    // Removed [Serializable] attribute - typically not needed for Razor ViewModels
    public class RestaurantRepHomeViewModel
    {
        // Public auto-implemented properties (simpler than backing fields)

        // Property for the welcome message
        public string WelcomeMessage { get; set; }

        // --- Restaurant Profile Section ---
        // Flag indicating if the rep has created a profile for their restaurant
        public bool HasProfile { get; set; }
        // Name of the restaurant (only relevant if HasProfile is true)
        public string RestaurantName { get; set; }
        // ID of the restaurant (only relevant if HasProfile is true)
        public int RestaurantId { get; set; }

        // --- Pending Reservations Section ---
        // Count of pending reservations (view only needs the count)
        public int PendingReservationCount { get; set; }
        // Note: Replaced List<Reservation> with just the count

        // --- Recent Reviews Section ---
        // List of recent reviews using ReviewDto for display purposes
        public List<ReviewDto> RecentReviews { get; set; }
        // Note: Changed from List<ReviewViewModel> to List<ReviewDto>

        // Constructor
        public RestaurantRepHomeViewModel()
        {
            // Initialize the list to avoid null reference exceptions in the view or controller
            RecentReviews = new List<ReviewDto>();
            // Set a default welcome message
            WelcomeMessage = "Welcome!";
            // Initialize other properties to default values if necessary
            HasProfile = false;
            RestaurantName = string.Empty;
            PendingReservationCount = 0;
        }
    }
}
