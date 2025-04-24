using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.ViewModels;

namespace Project3.WebApp.Controllers
{
    public class RestaurantCardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(RestaurantViewModel restaurant)
        {
            return View(restaurant);
        }
    }
} 