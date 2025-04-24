// Ensure this namespace matches your folder structure
namespace Project3.Shared.Models.Domain
{
    [Serializable]
    public class Review
    {
        // Backing fields
        private int _reviewID;
        private int _restaurantID;
        private int _userID;
        private string? _username;
        private DateTime _visitDate;
        private string? _comments;
        private int _foodQualityRating;
        private int _serviceRating;
        private int _atmosphereRating;
        private int _priceRating;
        private DateTime _createdDate;
        private DateTime? _modifiedDate;

        // Properties
        public int ReviewID
        {
            get { return _reviewID; }
            set { _reviewID = value; }
        }

        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }

        public int UserID
        {
            get { return _userID; }
            set { _userID = value; }
        }

        public string? Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public DateTime VisitDate
        {
            get { return _visitDate; }
            set { _visitDate = value; }
        }

        public string? Comments
        {
            get { return _comments; }
            set { _comments = value; }
        }

        public int FoodQualityRating
        {
            get { return _foodQualityRating; }
            set { _foodQualityRating = value; }
        }

        public int ServiceRating
        {
            get { return _serviceRating; }
            set { _serviceRating = value; }
        }

        public int AtmosphereRating
        {
            get { return _atmosphereRating; }
            set { _atmosphereRating = value; }
        }

        public int PriceRating
        {
            get { return _priceRating; }
            set { _priceRating = value; }
        }

        public DateTime CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }

        public DateTime? ModifiedDate
        {
            get { return _modifiedDate; }
            set { _modifiedDate = value; }
        }

        // Default constructor
        public Review()
        {
            _createdDate = DateTime.Now;
        }

        // Constructor with parameters
        public Review(int restaurantID, int userID, DateTime visitDate, string? comments,
                      int foodQualityRating, int serviceRating, int atmosphereRating, int priceRating)
        {
            _restaurantID = restaurantID;
            _userID = userID;
            _visitDate = visitDate;
            _comments = comments;
            _foodQualityRating = foodQualityRating;
            _serviceRating = serviceRating;
            _atmosphereRating = atmosphereRating;
            _priceRating = priceRating;
            _createdDate = DateTime.Now;
        }
    }
}
