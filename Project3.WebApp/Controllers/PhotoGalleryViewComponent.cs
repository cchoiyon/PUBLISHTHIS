using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.Domain;
using System.Collections.Generic;

namespace Project3.WebApp.Controllers
{
    public class PhotoGalleryViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(List<RestaurantImage> images, string title = "Photo Gallery")
        {
            ViewBag.GalleryTitle = title;
            
            // Optional: Add logic for pagination if there are many images
            if (images != null && images.Count > 20)
            {
                ViewBag.ShowPagination = true;
                ViewBag.TotalImages = images.Count;
                
                // Default to showing first 20 images
                return View(images.GetRange(0, 20));
            }
            
            return View(images);
        }
    }
} 