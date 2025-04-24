using System;
using System.Collections.Generic; // Required for List<>
using System.ComponentModel.DataAnnotations; // Required for validation attributes

// Ensure this namespace matches your project structure
namespace Project3.Shared.Models.ViewModels
{
    /// <summary>
    /// ViewModel/Input Model for the Reservation creation page (Reservation/Create view).
    /// Holds data needed for the form and includes validation attributes.
    /// NOTE: Uses explicit properties with private backing fields.
    /// </summary>
    [Serializable]
    public class ReservationViewModel
    {
        // Private backing fields
        private int _restaurantID;
        private string _restaurantName; // To display on the form
        private int? _userID; // Nullable for guest reservations
        private string _contactName;
        private string _phone;
        private string _email;
        private DateTime _reservationDateTime;
        private int _partySize;
        private string _specialRequests;

        // Public Properties
        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        public string RestaurantName // Read-only for the form display
        {
            get { return _restaurantName; }
            set { _restaurantName = value; } // Set by controller
        }

        public int? UserID // Hidden field or set by controller based on login status
        {
            get { return _userID; }
            set { _userID = value; }
        }

        [Required(ErrorMessage = "Contact Name is required.")]
        [StringLength(100)]
        [Display(Name = "Contact Name")]
        public string ContactName
        {
            get { return _contactName; }
            set { _contactName = value; }
        }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid Phone Number format.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address format.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        [Required(ErrorMessage = "Reservation Date and Time are required.")]
        [DataType(DataType.DateTime)]
        // Add custom validation attribute or controller logic to ensure date is in the future
        [Display(Name = "Reservation Date & Time")]
        public DateTime ReservationDateTime
        {
            get { return _reservationDateTime; }
            set { _reservationDateTime = value; }
        }

        [Required(ErrorMessage = "Party Size is required.")]
        [Range(1, 100, ErrorMessage = "Party Size must be between 1 and 100.")] // Example range
        [Display(Name = "Party Size")]
        public int PartySize
        {
            get { return _partySize; }
            set { _partySize = value; }
        }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Special Requests (Optional)")]
        public string SpecialRequests
        {
            get { return _specialRequests; }
            set { _specialRequests = value; }
        }

        // Parameterless constructor
        public ReservationViewModel() { }
    }
}
