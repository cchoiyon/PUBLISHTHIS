using System;
using System.Collections.Generic;
using Project3.Shared.Models.Domain;

namespace Project3.Shared.Models.ViewModels
{
    /// <summary>
    /// ViewModel for displaying a photo gallery
    /// </summary>
    public class PhotoGalleryViewModel
    {
        /// <summary>
        /// ID of the restaurant that owns the photos
        /// </summary>
        public int RestaurantId { get; set; }
        
        /// <summary>
        /// Name of the restaurant (for display purposes)
        /// </summary>
        public string RestaurantName { get; set; }
        
        /// <summary>
        /// Collection of photos in the gallery
        /// </summary>
        public IEnumerable<Photo> Photos { get; set; } = new List<Photo>();
        
        /// <summary>
        /// Upload model for adding new photos
        /// </summary>
        public PhotoUploadViewModel UploadModel { get; set; } = new PhotoUploadViewModel();
        
        /// <summary>
        /// Success message to display after an operation
        /// </summary>
        public string SuccessMessage { get; set; }
        
        /// <summary>
        /// Error message to display if an operation fails
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// Constructor that initializes the Photos collection
        /// </summary>
        public PhotoGalleryViewModel()
        {
            RestaurantName = string.Empty;
        }
    }

    public class PhotoUploadViewModel
    {
        /// <summary>
        /// ID of the restaurant that owns the photos
        /// </summary>
        public int RestaurantId { get; set; }
        
        /// <summary>
        /// Caption for the photo
        /// </summary>
        public string Caption { get; set; }
    }
} 