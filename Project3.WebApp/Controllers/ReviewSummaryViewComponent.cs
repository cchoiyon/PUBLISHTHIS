using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.ViewModels;
using System.Collections.Generic;

namespace Project3.WebApp.Controllers
{
    public class ReviewSummaryViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(List<ReviewViewModel> reviews)
        {
            // You can add any logic needed here to process the reviews
            // For example, calculating average ratings by category
            
            double avgFoodRating = 0;
            double avgServiceRating = 0;
            double avgAtmosphereRating = 0;
            double avgPriceRating = 0;
            double avgOverallRating = 0;
            
            if (reviews != null && reviews.Count > 0)
            {
                foreach (var review in reviews)
                {
                    avgFoodRating += review.FoodQualityRating;
                    avgServiceRating += review.ServiceRating;
                    avgAtmosphereRating += review.AtmosphereRating;
                    avgPriceRating += review.PriceRating;
                }
                
                int count = reviews.Count;
                avgFoodRating /= count;
                avgServiceRating /= count;
                avgAtmosphereRating /= count;
                avgPriceRating /= count;
                avgOverallRating = (avgFoodRating + avgServiceRating + avgAtmosphereRating + avgPriceRating) / 4;
            }
            
            ViewBag.AvgFoodRating = avgFoodRating;
            ViewBag.AvgServiceRating = avgServiceRating;
            ViewBag.AvgAtmosphereRating = avgAtmosphereRating;
            ViewBag.AvgPriceRating = avgPriceRating;
            ViewBag.AvgOverallRating = avgOverallRating;
            
            return View(reviews);
        }
    }
} 