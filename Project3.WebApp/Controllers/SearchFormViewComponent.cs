using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.ViewModels;
using System.Collections.Generic;

namespace Project3.WebApp.Controllers
{
    public class SearchFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(SearchCriteriaViewModel searchCriteria = null, List<string> availableCuisines = null)
        {
            // Create default model if none provided
            if (searchCriteria == null)
            {
                searchCriteria = new SearchCriteriaViewModel();
            }
            
            // Set available cuisines in the model
            if (availableCuisines != null && availableCuisines.Count > 0)
            {
                searchCriteria.AvailableCuisines = availableCuisines;
            }
            
            return View(searchCriteria);
        }
    }
} 