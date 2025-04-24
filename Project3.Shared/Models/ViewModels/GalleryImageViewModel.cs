using System;

namespace Project3.Shared.Models.ViewModels
{
    public class GalleryImageViewModel
    {
        public int ImageID { get; set; }
        public int RestaurantID { get; set; }
        public string ImagePath { get; set; }
        public string Caption { get; set; }
        public DateTime UploadDate { get; set; }
        public int DisplayOrder { get; set; }

        public GalleryImageViewModel()
        {
            ImagePath = string.Empty;
            Caption = string.Empty;
        }
    }
} 