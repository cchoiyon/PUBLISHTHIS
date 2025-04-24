using System;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace Project3.Shared.Models.ViewModels
{
    [Serializable]
    public class ReviewViewModel
    {
        private int _reviewID;
        private int _restaurantID;
        private int _userID;
        private DateTime _visitDate;
        private string _comments;
        private int _foodQualityRating;
        private int _serviceRating;
        private int _atmosphereRating;
        private int _priceRating;
        private DateTime _createdDate;
        private string _restaurantName;
        private string _reviewerUsername;

        public int ReviewID { get { return _reviewID; } set { _reviewID = value; } }
        public int RestaurantID { get { return _restaurantID; } set { _restaurantID = value; } }
        public int UserID { get { return _userID; } set { _userID = value; } }
        public DateTime VisitDate { get { return _visitDate; } set { _visitDate = value; } }
        public string Comments { get { return _comments; } set { _comments = value; } }
        public int FoodQualityRating { get { return _foodQualityRating; } set { _foodQualityRating = value; } }
        public int ServiceRating { get { return _serviceRating; } set { _serviceRating = value; } }
        public int AtmosphereRating { get { return _atmosphereRating; } set { _atmosphereRating = value; } }
        public int PriceRating { get { return _priceRating; } set { _priceRating = value; } }
        public DateTime CreatedDate { get { return _createdDate; } set { _createdDate = value; } }
        public string RestaurantName { get { return _restaurantName; } set { _restaurantName = value; } }
        public string ReviewerUsername { get { return _reviewerUsername; } set { _reviewerUsername = value; } }

        public ReviewViewModel() { }

        public ReviewViewModel(DataRow dr)
        {
            _reviewID = Convert.ToInt32(dr["ReviewID"]);
            _restaurantID = Convert.ToInt32(dr["RestaurantID"]);
            _userID = Convert.ToInt32(dr["UserID"]);
            _visitDate = Convert.ToDateTime(dr["VisitDate"]);
            _comments = dr["Comments"]?.ToString();
            _foodQualityRating = Convert.ToInt32(dr["FoodQualityRating"]);
            _serviceRating = Convert.ToInt32(dr["ServiceRating"]);
            _atmosphereRating = Convert.ToInt32(dr["AtmosphereRating"]);
            _priceRating = Convert.ToInt32(dr["PriceRating"]);
            _createdDate = Convert.ToDateTime(dr["CreatedDate"]);
            _restaurantName = dr.Table.Columns.Contains("RestaurantName") ? dr["RestaurantName"]?.ToString() : "N/A";
            _reviewerUsername = dr.Table.Columns.Contains("ReviewerUsername") ? dr["ReviewerUsername"]?.ToString() : "N/A";
        }
    }
}