using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Project3.Shared.Models.Domain;
using Project3.Shared.Models.InputModels;

namespace Project3.Shared.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Restaurant Details page (Restaurant/Details view).
    /// Combines profile, reviews, photos, and calculated display data.
    /// NOTE: Uses explicit properties with private backing fields.
    /// </summary>
    [Serializable]
    public class RestaurantDetailViewModel
    {
        // Private backing fields
        private int _restaurantID;
        private Restaurant _profile; // The full restaurant profile
        private List<ReviewViewModel> _reviews; // List of reviews for display
        private List<RestaurantImage> _photos; // List of photos for the gallery (using RestaurantImage domain model)
        private List<RestaurantImage> _galleryImages; // Gallery images with captions
        private string _averageRatingDisplay; // e.g., "4.5 / 5 stars"
        private string _averagePriceLevelDisplay; // e.g., "$$$"

        // Public Properties
        public int RestaurantID
        {
            get { return _restaurantID; }
            set { _restaurantID = value; }
        }   

        public Restaurant Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }

        public List<ReviewViewModel> Reviews
        {
            get { return _reviews; }
            set { _reviews = value; }
        }

        // Using RestaurantImage domain model here now
        public List<RestaurantImage> Photos
        {
            get { return _photos; }
            set { _photos = value; }
        }
        
        // Gallery images with captions
        public List<RestaurantImage> GalleryImages
        {
            get { return _galleryImages; }
            set { _galleryImages = value; }
        }

        public string AverageRatingDisplay
        {
            get { return _averageRatingDisplay; }
            set { _averageRatingDisplay = value; }
        }

        public string AveragePriceLevelDisplay
        {
            get { return _averagePriceLevelDisplay; }
            set { _averagePriceLevelDisplay = value; }
        }

        public int AverageRating { get; set; }

        // Parameterless constructor
        public RestaurantDetailViewModel()
        {
            // Initialize lists to prevent null reference errors in the view
            _profile = new Restaurant();
            _reviews = new List<ReviewViewModel>();
            _photos = new List<RestaurantImage>(); // Updated initialization
            _galleryImages = new List<RestaurantImage>();
        }
    }
}
