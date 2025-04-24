using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Project3.Shared.Models.Domain;

namespace Project3.Shared.Models.ViewModels
{
    /// <summary>
    /// ViewModel representing restaurant data for display (e.g., search results, cards)
    /// AND for editing in the profile manager form. Includes calculated fields.
    /// Uses explicit properties style.
    /// </summary>
    [Serializable]
    public class RestaurantViewModel
    {
        // --- Fields from Restaurant domain model ---
        private int _restaurantID;
        private string _name;
        private string _address;
        private string _city;
        private string _state;
        private string _zipCode;
        private string _cuisine;
        private string _hours;
        private string _contact;
        private string _profilePhoto;
        private string _logoPhoto;
        private string _marketingDescription;
        private string _websiteURL;
        private string _socialMedia;
        private string _owner;
        private string _description;
        private string _email;
        private string _priceRange;

        // --- Calculated/Aggregated fields (populated by API or Controller) ---
        private double _overallRating;
        private int _reviewCount;
        private double _averagePriceRating;

        // --- Properties ---

        // Identifiers
        public int RestaurantID { get { return _restaurantID; } set { _restaurantID = value; } }

        // Core Info (Editable in ManageProfile)
        [Required(ErrorMessage = "Restaurant name is required")]
        [StringLength(100, ErrorMessage = "Restaurant name cannot exceed 100 characters")]
        public string Name { get { return _name; } set { _name = value; } }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get { return _description; } set { _description = value; } }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string Address { get { return _address; } set { _address = value; } }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string City { get { return _city; } set { _city = value; } }

        [StringLength(2, ErrorMessage = "State must be 2 characters")]
        public string State { get { return _state; } set { _state = value; } }

        [StringLength(10, ErrorMessage = "ZIP code cannot exceed 10 characters")]
        public string ZipCode { get { return _zipCode; } set { _zipCode = value; } }

        [StringLength(50, ErrorMessage = "Cuisine cannot exceed 50 characters")]
        public string Cuisine { get { return _cuisine; } set { _cuisine = value; } }

        [StringLength(500, ErrorMessage = "Hours cannot exceed 500 characters")]
        public string Hours { get { return _hours; } set { _hours = value; } }

        [StringLength(100, ErrorMessage = "Contact information cannot exceed 100 characters")]
        public string Contact { get { return _contact; } set { _contact = value; } }

        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get { return _email; } set { _email = value; } }

        [StringLength(1000, ErrorMessage = "Marketing description cannot exceed 1000 characters")]
        public string MarketingDescription { get { return _marketingDescription; } set { _marketingDescription = value; } }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters")]
        public string WebsiteURL { get { return _websiteURL; } set { _websiteURL = value; } }

        [StringLength(500, ErrorMessage = "Social media links cannot exceed 500 characters")]
        public string SocialMedia { get { return _socialMedia; } set { _socialMedia = value; } }

        [StringLength(100, ErrorMessage = "Owner name cannot exceed 100 characters")]
        public string Owner { get { return _owner; } set { _owner = value; } }

        [StringLength(50, ErrorMessage = "Price range cannot exceed 50 characters")]
        public string PriceRange { get { return _priceRange; } set { _priceRange = value; } }

        // Photo URLs (Primary ones stored in TP_Restaurants)
        public string? ProfilePhoto { get; set; }
        public string? LogoPhoto { get; set; }

        // Calculated/Aggregated Properties (Read-only in forms, set by Controller/API)
        [Display(Name = "Overall Rating")]
        public double OverallRating { get { return _overallRating; } set { _overallRating = value; } } // Typically set from API result

        [Display(Name = "Number of Reviews")]
        public int ReviewCount { get { return _reviewCount; } set { _reviewCount = value; } } // Typically set from API result

        [Display(Name = "Average Price")]
        public double AveragePriceRating { get { return _averagePriceRating; } set { _averagePriceRating = value; } } // Typically set from API result

        public double AverageRating { get; set; }
        public int AveragePriceLevel { get; set; }

        // File upload properties - added for form uploads
        public IFormFile? ProfilePhotoFile { get; set; }
        public IFormFile? LogoPhotoFile { get; set; }
        
        // Gallery image upload property - multiple files
        [Display(Name = "Gallery Images")]
        public List<IFormFile>? GalleryImageFiles { get; set; } = new List<IFormFile>();

        // File path properties
        public string? ProfilePhotoFilePath { get; set; }
        public string? LogoPhotoFilePath { get; set; }

        // Gallery images collection property
        public List<GalleryImageViewModel> GalleryImages { get; set; } = new List<GalleryImageViewModel>();

        // Constructor
        public RestaurantViewModel() 
        {
            // Initialize string fields to empty strings to avoid null reference warnings
            _name = string.Empty;
            _address = string.Empty;
            _city = string.Empty;
            _state = string.Empty;
            _zipCode = string.Empty;
            _cuisine = string.Empty;
            _hours = string.Empty;
            _contact = string.Empty;
            _profilePhoto = string.Empty;
            _logoPhoto = string.Empty;
            _marketingDescription = string.Empty;
            _websiteURL = string.Empty;
            _socialMedia = string.Empty;
            _owner = string.Empty;
            _description = string.Empty;
            _email = string.Empty;
            _priceRange = string.Empty;

            // Initialize empty list for GalleryImages
            GalleryImages = new List<GalleryImageViewModel>();
        }

        // You might add a constructor that takes a Restaurant domain object
        // public RestaurantViewModel(Restaurant restaurant) { /* map properties */ }

        // You could also add mapping methods here or use a library like AutoMapper
    }
}
