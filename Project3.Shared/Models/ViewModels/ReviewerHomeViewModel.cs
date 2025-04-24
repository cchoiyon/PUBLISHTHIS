using System;
using System.Collections.Generic;

namespace Project3.Shared.Models.ViewModels
{
    [Serializable]
    public class ReviewerHomeViewModel
    {
        // --- Private backing fields ---

        // Fields for dashboard elements
        private string _welcomeMessage;
        private int _totalReviewsCount;

        // Existing fields for search/display
        private List<RestaurantViewModel> _featuredRestaurants;
        private List<RestaurantViewModel> _searchResults;
        private SearchCriteriaViewModel _searchCriteria; // To repopulate search form
        private List<string> _availableCuisines; // For populating checkboxes


        // --- Public properties ---

        // Properties for dashboard elements
        public string WelcomeMessage
        {
            get { return _welcomeMessage; }
            set { _welcomeMessage = value; }
        }
        public int TotalReviewsCount
        {
            get { return _totalReviewsCount; }
            set { _totalReviewsCount = value; }
        }

        // Existing properties for search/display
        public List<RestaurantViewModel> FeaturedRestaurants
        {
            get { return _featuredRestaurants; }
            set { _featuredRestaurants = value; }
        }
        public List<RestaurantViewModel> SearchResults
        {
            get { return _searchResults; }
            set { _searchResults = value; }
        }
        public SearchCriteriaViewModel SearchCriteria
        {
            get { return _searchCriteria; }
            set { _searchCriteria = value; }
        }
        public List<string> AvailableCuisines
        {
            get { return _availableCuisines; }
            set { _availableCuisines = value; }
        }


        // --- Constructor ---
        public ReviewerHomeViewModel()
        {
            // Initialize dashboard fields
            _welcomeMessage = "Welcome, Reviewer!"; // Default welcome
            _totalReviewsCount = 0;

            // Initialize existing fields to avoid null reference errors in the view
            _featuredRestaurants = new List<RestaurantViewModel>();
            _searchResults = new List<RestaurantViewModel>();
            _searchCriteria = new SearchCriteriaViewModel();
            _availableCuisines = new List<string>(); // Populate this in the controller
        }
    }
}
