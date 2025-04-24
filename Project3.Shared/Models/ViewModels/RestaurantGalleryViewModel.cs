using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Project3.Shared.Models.Domain;

namespace Project3.Shared.Models.ViewModels
{
    public class RestaurantGalleryViewModel
    {
        public int RestaurantID { get; set; }
        public string RestaurantName { get; set; }
        public List<RestaurantImage> GalleryImages { get; set; }
        
        public RestaurantGalleryViewModel()
        {
            GalleryImages = new List<RestaurantImage>();
        }
    }
    
    public class UploadGalleryImageViewModel
    {
        public int RestaurantID { get; set; }
        
        [Required(ErrorMessage = "Please select an image to upload")]
        [Display(Name = "Gallery Image")]
        public IFormFile ImageFile { get; set; }
        
        [StringLength(200, ErrorMessage = "Caption cannot exceed 200 characters")]
        [Display(Name = "Image Caption/Description")]
        public string Caption { get; set; }
    }
    
    public class UpdateImageCaptionViewModel
    {
        public int ImageID { get; set; }
        public int RestaurantID { get; set; }
        
        [Required(ErrorMessage = "Caption is required")]
        [StringLength(200, ErrorMessage = "Caption cannot exceed 200 characters")]
        public string Caption { get; set; }
    }
} 