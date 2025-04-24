using System;
using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class Reservation
    {
        // Backing fields
        private int _reservationID;
        private int _restaurantID;
        private int? _userID;
        private DateTime _reservationDateTime;
        private int _partySize;
        private string? _contactName; 
        private string? _phone; 
        private string? _email; 
        private string? _specialRequests; 
        private string _status; 
        private DateTime _createdDate;

        // Properties
        public int ReservationID
        {
            get { return _reservationID; }
            set { _reservationID = value; }
        }

        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        public int? UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        [Display(Name = "Date & Time")]
        public DateTime ReservationDateTime
        {
            get { return _reservationDateTime; }
            set { _reservationDateTime = value; }
        }

        [Display(Name = "Party Size")]
        public int PartySize
        {
            get { return _partySize; }
            set { _partySize = value; }
        }

        [Display(Name = "Contact Name")]
        public string? ContactName 
        {
            get { return _contactName; }
            set { _contactName = value; }
        }

        [Display(Name = "Email")]
        public string? ContactEmail
        {
            get { return _email; }
            set { _email = value; }
        }

        [Display(Name = "Phone")]
        public string? ContactPhone
        {
            get { return _phone; }
            set { _phone = value; }
        }

        [Display(Name = "Special Requests")]
        public string? SpecialRequests 
        {
            get { return _specialRequests; }
            set { _specialRequests = value; }
        }

        [Display(Name = "Status")]
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }

        // Default constructor
        public Reservation()
        {
            _status = "Pending";
            _createdDate = DateTime.Now;
        }

        // Constructor with parameters
        public Reservation(int restaurantID, int? userID, DateTime reservationDateTime, int partySize,
                           string contactName, string phone, string email, string? specialRequests, string status = "Pending")
        {
            _restaurantID = restaurantID;
            _userID = userID;
            _reservationDateTime = reservationDateTime;
            _partySize = partySize;
            _contactName = contactName;
            _phone = phone;
            _email = email;
            _specialRequests = specialRequests;
            _status = status;
            _createdDate = DateTime.Now;
        }
    }
}
