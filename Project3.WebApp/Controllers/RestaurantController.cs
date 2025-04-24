using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory; // Add for caching
using Project3.Shared.Models.Domain; // For Restaurant, RestaurantImage
using Project3.Shared.Models.ViewModels; // For RestaurantDetailViewModel, ReviewViewModel, SearchCriteriaViewModel
using Project3.Shared.Services;
using Project3.Shared.Utilities;
using System.Data;
using System.Data.SqlClient;

namespace Project3.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ILogger<RestaurantController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Connection _dbConnect;
        private readonly IMemoryCache _cache; // Add cache field
        private readonly FileStorageService _fileStorageService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private const string CuisinesCacheKey = "AllCuisines"; // Cache key for cuisines

        public RestaurantController(
            ILogger<RestaurantController> logger, 
            IHttpClientFactory httpClientFactory, 
            Connection dbConnect,
            IMemoryCache cache,
            IWebHostEnvironment hostEnvironment,
            IConfiguration configuration) 
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _dbConnect = dbConnect;
            _cache = cache;
            _hostEnvironment = hostEnvironment;
            _fileStorageService = new FileStorageService(hostEnvironment.ContentRootPath, configuration);
        }

        // GET: /Restaurant/Details/5
        // Fetches profile, reviews, and photos from database to display details.
        public IActionResult Details(int id) // id is RestaurantID
        {
            if (id <= 0)
            {
                _logger.LogWarning("Details requested with invalid ID: {RestaurantId}", id);
                return NotFound(); // Return 404 for invalid ID
            }

            try
            {
                _logger.LogInformation($"Loading details for restaurant ID: {id}");
                
                // Get restaurant details using stored procedure
                var cmd = new SqlCommand("dbo.TP_spGetRestaurantById");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds?.Tables.Count == 0 || ds?.Tables[0]?.Rows.Count == 0)
                {
                    _logger.LogWarning("Restaurant not found with ID: {RestaurantId}", id);
                    return NotFound();
                }

                var row = ds.Tables[0].Rows[0];
                var restaurant = new Restaurant
                {
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Address = row["Address"]?.ToString() ?? string.Empty,
                    City = row["City"]?.ToString() ?? string.Empty,
                    State = row["State"]?.ToString() ?? string.Empty,
                    ZipCode = row["ZipCode"]?.ToString() ?? string.Empty,
                    Cuisine = row["Cuisine"]?.ToString() ?? string.Empty,
                    Hours = row["Hours"]?.ToString() ?? string.Empty,
                    Contact = row["Contact"]?.ToString() ?? string.Empty,
                    MarketingDescription = row["MarketingDescription"]?.ToString() ?? string.Empty,
                    WebsiteURL = row["WebsiteURL"]?.ToString() ?? string.Empty,
                    SocialMedia = row["SocialMedia"]?.ToString() ?? string.Empty,
                    Owner = row["Owner"]?.ToString() ?? string.Empty,
                    ProfilePhoto = row["ProfilePhoto"]?.ToString() ?? string.Empty,
                    LogoPhoto = row["LogoPhoto"]?.ToString() ?? string.Empty
                };

                // Get reviews using stored procedure
                var reviewCmd = new SqlCommand("dbo.TP_spGetRestaurantReviews");
                reviewCmd.CommandType = CommandType.StoredProcedure;
                reviewCmd.Parameters.AddWithValue("@RestaurantID", id);
                var reviewDs = _dbConnect.GetDataSetUsingCmdObj(reviewCmd);
                var reviews = new List<ReviewViewModel>();

                if (reviewDs?.Tables.Count > 0 && reviewDs?.Tables[0]?.Rows.Count > 0)
                {
                    foreach (DataRow reviewRow in reviewDs.Tables[0].Rows)
                    {
                        reviews.Add(new ReviewViewModel
                        {
                            ReviewID = Convert.ToInt32(reviewRow["ReviewID"]),
                            RestaurantID = Convert.ToInt32(reviewRow["RestaurantID"]),
                            UserID = Convert.ToInt32(reviewRow["UserID"]),
                            ReviewerUsername = reviewRow["ReviewerUsername"]?.ToString() ?? "Anonymous",
                            VisitDate = Convert.ToDateTime(reviewRow["VisitDate"]),
                            FoodQualityRating = Convert.ToInt32(reviewRow["FoodQualityRating"]),
                            ServiceRating = Convert.ToInt32(reviewRow["ServiceRating"]),
                            AtmosphereRating = Convert.ToInt32(reviewRow["AtmosphereRating"]),
                            PriceRating = Convert.ToInt32(reviewRow["PriceRating"]),
                            Comments = reviewRow["Comments"]?.ToString() ?? string.Empty
                        });
                    }
                }

                // Get gallery images using stored procedure
                var galleryCmd = new SqlCommand("dbo.TP_spGetRestaurantGalleryImages");
                galleryCmd.CommandType = CommandType.StoredProcedure;
                galleryCmd.Parameters.AddWithValue("@RestaurantID", id);
                var galleryDs = _dbConnect.GetDataSetUsingCmdObj(galleryCmd);
                var galleryImages = new List<RestaurantImage>();
                
                if (galleryDs?.Tables.Count > 0 && galleryDs.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow galleryRow in galleryDs.Tables[0].Rows)
                    {
                        galleryImages.Add(new RestaurantImage
                        {
                            ImageID = Convert.ToInt32(galleryRow["ImageID"]),
                            RestaurantID = Convert.ToInt32(galleryRow["RestaurantID"]),
                            ImagePath = galleryRow["ImagePath"]?.ToString() ?? string.Empty,
                            Caption = galleryRow["Caption"]?.ToString() ?? string.Empty,
                            UploadDate = Convert.ToDateTime(galleryRow["UploadDate"]),
                            DisplayOrder = galleryRow["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(galleryRow["DisplayOrder"]) : 0
                        });
                    }
                }

                // Calculate average rating and price level
                double averageRating = CalculateAverageRating(reviews);
                int averagePriceLevel = CalculateAveragePriceLevel(reviews);

                // Create view model
                var viewModel = new RestaurantDetailViewModel
                {
                    RestaurantID = id,
                    Profile = restaurant,
                    Reviews = reviews,
                    GalleryImages = galleryImages,
                    AverageRatingDisplay = GetStars(averageRating),
                    AveragePriceLevelDisplay = GetPriceLevel(averagePriceLevel),
                    AverageRating = (int)Math.Round(averageRating)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for Restaurant ID {RestaurantId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred loading restaurant details.";
                return RedirectToAction("Search");
            }
        }

        // Add private method to get cuisines
        private List<string> GetCuisinesList()
        {
            // Try to get cuisines from cache first
            if (_cache.TryGetValue(CuisinesCacheKey, out List<string> cachedCuisines))
            {
                return cachedCuisines;
            }

            var cuisines = new List<string>();
            try
            {
                // Use direct SQL query instead of stored procedure
                var cmd = new SqlCommand(@"
                    SELECT DISTINCT Cuisine 
                    FROM TP_Restaurants 
                    WHERE Cuisine IS NOT NULL AND Cuisine != '' 
                    ORDER BY Cuisine");
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows != null)
                {
                    cuisines = ds.Tables[0].Rows
                        .Cast<DataRow>()
                        .Where(row => row["Cuisine"] != DBNull.Value)
                        .Select(row => row["Cuisine"].ToString())
                        .Where(cuisine => !string.IsNullOrWhiteSpace(cuisine))
                        .ToList();

                    // Add some common cuisines if the list is very small
                    if (cuisines.Count < 5)
                    {
                        var commonCuisines = new[] { 
                            "American", "Italian", "Mexican", "Chinese", "Japanese", 
                            "Indian", "Thai", "Mediterranean", "French", "Greek" 
                        };
                        
                        foreach (var cuisine in commonCuisines)
                        {
                            if (!cuisines.Contains(cuisine, StringComparer.OrdinalIgnoreCase))
                            {
                                cuisines.Add(cuisine);
                            }
                        }
                    }

                    // Cache the cuisines for 1 hour
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    _cache.Set(CuisinesCacheKey, cuisines, cacheOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cuisines list");
            }

            return cuisines;
        }

        // GET: /Restaurant/Search
        public IActionResult Search()
        {
            var model = new SearchCriteriaViewModel
            {
                AvailableCuisines = GetCuisinesList()
            };
            return View(model);
        }

        // POST: /Restaurant/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Search(SearchCriteriaViewModel searchCriteria)
        {
            try {
                // Initialize if null
                searchCriteria ??= new SearchCriteriaViewModel();
                
                // Always repopulate the cuisines list
                searchCriteria.AvailableCuisines = GetCuisinesList();
                
                // Check if this is an empty search (no filters selected)
                bool isEmptySearch = (searchCriteria.SelectedCuisines == null || !searchCriteria.SelectedCuisines.Any()) &&
                                   string.IsNullOrWhiteSpace(searchCriteria.CuisineInput) &&
                                   string.IsNullOrWhiteSpace(searchCriteria.City) &&
                                   string.IsNullOrWhiteSpace(searchCriteria.State);
                
                // For empty searches, use the stored procedure with NULL parameters
                // This ensures the stored procedure returns all restaurants when all parameters are NULL
                var searchResults = new List<RestaurantViewModel>();
                var cmd = new SqlCommand("dbo.TP_spSearchRestaurants");
                cmd.CommandType = CommandType.StoredProcedure;
                
                // Handle multiple cuisine selections by joining them into a comma-separated string
                string cuisineList = null;
                if (searchCriteria.SelectedCuisines != null && searchCriteria.SelectedCuisines.Any())
                {
                    // Join selected cuisines with commas for the stored procedure
                    cuisineList = string.Join(",", searchCriteria.SelectedCuisines.Where(c => !string.IsNullOrWhiteSpace(c)));
                }
                // Fall back to single cuisine input if multiple selection is empty but single selection has value
                else if (!string.IsNullOrWhiteSpace(searchCriteria.CuisineInput))
                {
                    cuisineList = searchCriteria.CuisineInput.Trim();
                }

                _logger.LogInformation($"Searching with: Cuisines={cuisineList}, City={searchCriteria.City}, State={searchCriteria.State}");

                // Add parameters only if they have values, otherwise use DBNull.Value
                cmd.Parameters.AddWithValue("@CuisineList", 
                    !string.IsNullOrWhiteSpace(cuisineList) 
                        ? cuisineList 
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@City", 
                    !string.IsNullOrWhiteSpace(searchCriteria.City) 
                        ? searchCriteria.City.Trim() 
                        : (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@State", 
                    !string.IsNullOrWhiteSpace(searchCriteria.State) 
                        ? searchCriteria.State.Trim().ToUpper() 
                        : (object)DBNull.Value);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        string cuisine = row["Cuisine"]?.ToString() ?? string.Empty;
                        string imagePath = "/images/restaurant-placeholder.png";
                        
                        // Try to use cuisine-specific image if available
                        if (!string.IsNullOrEmpty(cuisine))
                        {
                            string cuisineImagePath = $"/images/restaurants/{cuisine.ToLower().Replace(" ", "-")}-restaurant.jpg";
                            
                            // Check if the file exists safely
                            try 
                            {
                                string physicalPath = Path.Combine(_hostEnvironment.WebRootPath, cuisineImagePath.TrimStart('/'));
                                if (System.IO.File.Exists(physicalPath))
                                {
                                    imagePath = cuisineImagePath;
                                }
                            }
                            catch (Exception ex) 
                            {
                                _logger.LogWarning(ex, "Error checking for cuisine image file");
                            }
                        }
                        
                        // Use LogoPhoto if available, otherwise use cuisine-specific or default image
                        if (!string.IsNullOrEmpty(row["LogoPhoto"]?.ToString()))
                        {
                            imagePath = row["LogoPhoto"].ToString();
                        }
                        
                        var restaurant = new RestaurantViewModel
                        {
                            RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                            Name = row["Name"]?.ToString() ?? string.Empty,
                            Cuisine = cuisine,
                            City = row["City"]?.ToString() ?? string.Empty,
                            State = row["State"]?.ToString() ?? string.Empty,
                            Address = row["Address"]?.ToString() ?? string.Empty,
                            ProfilePhoto = imagePath,
                            AverageRating = row["OverallRating"] != DBNull.Value ? (int)Math.Round(Convert.ToDouble(row["OverallRating"])) : 0,
                            ReviewCount = row["ReviewCount"] != DBNull.Value ? Convert.ToInt32(row["ReviewCount"]) : 0,
                            AveragePriceLevel = row["AveragePriceRating"] != DBNull.Value ? (int)Math.Round(Convert.ToDouble(row["AveragePriceRating"])) : 0
                        };
                        searchResults.Add(restaurant);
                    }
                }

                // Display results directly - avoid using TempData which may cause issues
                ViewBag.SearchResults = searchResults;
                
                if (searchResults.Count == 0)
                {
                    ViewBag.Message = "No restaurants found matching your search criteria.";
                }
                else if (isEmptySearch)
                {
                    ViewBag.Message = $"Showing all {searchResults.Count} restaurants.";
                }
                else
                {
                    ViewBag.Message = $"Found {searchResults.Count} restaurants matching your criteria.";
                }
                
                return View("SearchResults", searchCriteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching restaurants");
                ViewBag.Error = "An error occurred while searching for restaurants. Please try again.";
                searchCriteria ??= new SearchCriteriaViewModel();
                searchCriteria.AvailableCuisines = GetCuisinesList();
                return View(searchCriteria);
            }
        }

        // GET: /Restaurant/SearchResults
        public IActionResult SearchResults()
        {
            try
            {
                var model = new SearchCriteriaViewModel
                {
                    AvailableCuisines = GetCuisinesList()
                };

                // Set empty results if this is a direct access
                ViewBag.SearchResults = new List<RestaurantViewModel>();
                ViewBag.Message = "Please use the search form to find restaurants.";
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SearchResults action");
                ViewBag.Error = "An unexpected error occurred while displaying search results. Please try again.";
                return RedirectToAction(nameof(Search));
            }
        }

        // --- Helper methods ---
        // NOTE: Consider moving these calculation/formatting helpers into the
        // RestaurantDetailViewModel class or a dedicated utility/service class
        // to improve separation of concerns and align better with component design.

        private double CalculateAverageRating(List<ReviewViewModel> reviews)
        {
            if (reviews == null || !reviews.Any()) return 0;
            // Assuming ReviewViewModel has FoodQualityRating, ServiceRating, AtmosphereRating
            try
            {
                // Using Average() handles potential empty list implicitly if called after Any() check
                return reviews.Average(r => (r.FoodQualityRating + r.ServiceRating + r.AtmosphereRating) / 3.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating.");
                return 0; // Return default on error
            }
        }

        private int CalculateAveragePriceLevel(List<ReviewViewModel> reviews)
        {
            if (reviews == null || !reviews.Any()) return 0;
            // Assuming ReviewViewModel has PriceRating
            try
            {
                // Using Average() handles potential empty list implicitly if called after Any() check
                return (int)Math.Round(reviews.Average(r => r.PriceRating));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average price level.");
                return 0; // Return default on error
            }
        }

        private string GetStars(double rating)
        {
            // Example: Return simple text - View should handle rendering actual stars
            return $"{Math.Round(rating, 1)} / 5";
        }

        private string GetPriceLevel(int priceLevel)
        {
            // Example: Return dollar signs - View can use this directly
            priceLevel = Math.Max(1, Math.Min(5, priceLevel)); // Ensure 1-5
            return new string('$', priceLevel);
        }
        // --- End Helper Methods ---

        // Helper method to update restaurant images based on cuisine
        private void UpdateRestaurantImages()
        {
            try
            {
                // Dictionary mapping cuisine keywords to image paths
                var cuisineImageMap = new Dictionary<string, string>
                {
                    { "italian", "/images/restaurants/italian-restaurant.jpg" },
                    { "mexican", "/images/restaurants/mexican-restaurant.jpg" },
                    { "chinese", "/images/restaurants/chinese-restaurant.jpg" },
                    { "japanese", "/images/restaurants/japanese-restaurant.jpg" },
                    { "american", "/images/restaurants/american-restaurant.jpg" },
                    { "indian", "/images/restaurants/indian-restaurant.jpg" },
                    { "thai", "/images/restaurants/thai-restaurant.jpg" },
                    { "mediterranean", "/images/restaurants/mediterranean-restaurant.jpg" }
                };

                // Default image for restaurants without a matching cuisine
                string defaultImage = "/images/restaurant-placeholder.png";

                // Get all restaurants
                var cmd = new SqlCommand("SELECT RestaurantID, Cuisine FROM TP_Restaurants");
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                if (ds?.Tables[0]?.Rows != null)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        int restaurantId = Convert.ToInt32(row["RestaurantID"]);
                        string cuisine = row["Cuisine"]?.ToString()?.ToLower() ?? string.Empty;
                        
                        // Find matching image path
                        string imagePath = defaultImage;
                        foreach (var kvp in cuisineImageMap)
                        {
                            if (cuisine.Contains(kvp.Key))
                            {
                                imagePath = kvp.Value;
                                break;
                            }
                        }
                        
                        // Update the restaurant's LogoPhoto
                        var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET LogoPhoto = @LogoPhoto WHERE RestaurantID = @RestaurantID");
                        updateCmd.Parameters.AddWithValue("@LogoPhoto", imagePath);
                        updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                        _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                    }
                }
                
                _logger.LogInformation("Restaurant images updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating restaurant images");
            }
        }

        // Action to manually update restaurant images (for testing)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateImages()
        {
            UpdateRestaurantImages();
            TempData["Message"] = "Restaurant images updated successfully";
            return RedirectToAction(nameof(Search));
        }

        // GET: /Restaurant/ManageImages/5
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ManageImages(int id)
        {
            try
            {
                // Get the restaurant details
                var cmd = new SqlCommand("SELECT * FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows.Count == 0)
                {
                    TempData["Error"] = "Restaurant not found.";
                    return RedirectToAction("Index", "Home");
                }
                
                var row = ds.Tables[0].Rows[0];
                var restaurant = new RestaurantViewModel
                {
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Cuisine = row["Cuisine"]?.ToString() ?? string.Empty,
                    City = row["City"]?.ToString() ?? string.Empty,
                    State = row["State"]?.ToString() ?? string.Empty,
                    Address = row["Address"]?.ToString() ?? string.Empty,
                    ProfilePhoto = row["ProfilePhoto"]?.ToString() ?? string.Empty,
                    LogoPhoto = row["LogoPhoto"]?.ToString() ?? string.Empty
                };
                
                return View(restaurant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading restaurant images for ID {RestaurantId}", id);
                TempData["Error"] = "An error occurred while loading restaurant images.";
                return RedirectToAction("Index", "Home");
            }
        }
        
        // POST: /Restaurant/UploadProfilePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public async Task<IActionResult> UploadProfilePhoto(int restaurantId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only JPG, JPEG, and PNG files are allowed.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Validate file size (max 2MB)
                if (imageFile.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 2MB.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Save file using FileStorageService
                var relativePath = await _fileStorageService.SaveRestaurantProfilePhotoAsync(imageFile, restaurantId);
                
                // Update database with relative path
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET ProfilePhoto = @ProfilePhoto WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@ProfilePhoto", relativePath);
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Profile photo uploaded successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while uploading the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
        
        // POST: /Restaurant/UploadLogoPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public async Task<IActionResult> UploadLogoPhoto(int restaurantId, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only JPG, JPEG, and PNG files are allowed.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Validate file size (max 2MB)
                if (imageFile.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 2MB.";
                    return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
                }
                
                // Save file using FileStorageService
                var relativePath = await _fileStorageService.SaveRestaurantLogoPhotoAsync(imageFile, restaurantId);
                
                // Update database with relative path
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET LogoPhoto = @LogoPhoto WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@LogoPhoto", relativePath);
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Logo photo uploaded successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while uploading the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
        
        // POST: /Restaurant/DeleteProfilePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult DeleteProfilePhoto(int restaurantId)
        {
            try
            {
                // Get the current profile photo path
                var cmd = new SqlCommand("SELECT ProfilePhoto FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows.Count > 0)
                {
                    var photoPath = ds.Tables[0].Rows[0]["ProfilePhoto"]?.ToString();
                    
                    // Delete the file if it exists
                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        _fileStorageService.DeleteFile(photoPath);
                    }
                }
                
                // Update database to remove the photo path
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET ProfilePhoto = NULL WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Profile photo deleted successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while deleting the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }
        
        // POST: /Restaurant/DeleteLogoPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult DeleteLogoPhoto(int restaurantId)
        {
            try
            {
                // Get the current logo photo path
                var cmd = new SqlCommand("SELECT LogoPhoto FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables[0]?.Rows.Count > 0)
                {
                    var photoPath = ds.Tables[0].Rows[0]["LogoPhoto"]?.ToString();
                    
                    // Delete the file if it exists
                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        _fileStorageService.DeleteFile(photoPath);
                    }
                }
                
                // Update database to remove the photo path
                var updateCmd = new SqlCommand("UPDATE TP_Restaurants SET LogoPhoto = NULL WHERE RestaurantID = @RestaurantID");
                updateCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                TempData["Message"] = "Logo photo deleted successfully.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting logo photo for restaurant ID {RestaurantId}", restaurantId);
                TempData["Error"] = "An error occurred while deleting the image.";
                return RedirectToAction(nameof(ManageImages), new { id = restaurantId });
            }
        }

        // GET: /Restaurant/ManageGallery/{id}
        [HttpGet]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ManageGallery(int id)
        {
            try
            {
                // Get restaurant details
                var cmd = new SqlCommand("SELECT Name FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                if (ds?.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    TempData["Error"] = "Restaurant not found.";
                    return RedirectToAction("Index", "Home");
                }
                
                var restaurantName = ds.Tables[0].Rows[0]["Name"].ToString();
                
                // Get gallery images
                var galleryCmd = new SqlCommand("TP_spGetRestaurantGalleryImages");
                galleryCmd.CommandType = CommandType.StoredProcedure;
                galleryCmd.Parameters.AddWithValue("@RestaurantID", id);
                var galleryDs = _dbConnect.GetDataSetUsingCmdObj(galleryCmd);
                
                var viewModel = new RestaurantGalleryViewModel
                {
                    RestaurantID = id,
                    RestaurantName = restaurantName,
                    GalleryImages = new List<RestaurantImage>()
                };
                
                if (galleryDs?.Tables.Count > 0 && galleryDs.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in galleryDs.Tables[0].Rows)
                    {
                        viewModel.GalleryImages.Add(new RestaurantImage
                        {
                            ImageID = Convert.ToInt32(row["ImageID"]),
                            RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                            ImagePath = row["ImagePath"].ToString(),
                            Caption = row["Caption"]?.ToString(),
                            UploadDate = Convert.ToDateTime(row["UploadDate"]),
                            DisplayOrder = Convert.ToInt32(row["DisplayOrder"])
                        });
                    }
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading gallery for restaurant ID {RestaurantId}", id);
                TempData["Error"] = "An error occurred while loading the gallery.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Restaurant/UploadGalleryImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public async Task<IActionResult> UploadGalleryImage(UploadGalleryImageViewModel model)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                TempData["Error"] = "Please select an image to upload.";
                return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
            }
            
            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only JPG, JPEG, and PNG files are allowed.";
                    return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
                }
                
                // Validate file size (max 5MB)
                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
                }
                
                // Save file using FileStorageService
                var relativePath = await _fileStorageService.SaveRestaurantGalleryImageAsync(model.ImageFile, model.RestaurantID);
                
                // Add record to database
                var cmd = new SqlCommand("TP_spAddRestaurantGalleryImage");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                cmd.Parameters.AddWithValue("@ImagePath", relativePath);
                cmd.Parameters.AddWithValue("@Caption", model.Caption ?? "");
                _dbConnect.DoUpdateUsingCmdObj(cmd);
                
                TempData["Message"] = "Gallery image uploaded successfully.";
                return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading gallery image for restaurant ID {RestaurantId}", model.RestaurantID);
                TempData["Error"] = "An error occurred while uploading the image.";
                return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
            }
        }

        // POST: /Restaurant/UpdateGalleryImageCaption
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult UpdateGalleryImageCaption(UpdateImageCaptionViewModel model)
        {
            try
            {
                var cmd = new SqlCommand("TP_spUpdateGalleryImage");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ImageID", model.ImageID);
                cmd.Parameters.AddWithValue("@Caption", model.Caption);
                cmd.Parameters.AddWithValue("@DisplayOrder", DBNull.Value); // Pass null for DisplayOrder since we're only updating caption
                _dbConnect.DoUpdateUsingCmdObj(cmd);
                
                TempData["Message"] = "Caption updated successfully.";
                return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating caption for image ID {ImageId}", model.ImageID);
                TempData["Error"] = "An error occurred while updating the caption.";
                return RedirectToAction(nameof(ManageGallery), new { id = model.RestaurantID });
            }
        }

        // POST: /Restaurant/DeleteGalleryImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult DeleteGalleryImage(int imageId, int restaurantId)
        {
            try
            {
                // Get the image path before deleting the record
                var cmd = new SqlCommand("TP_spDeleteGalleryImage");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ImageID", imageId);
                
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    var photoPath = ds.Tables[0].Rows[0]["ImagePath"]?.ToString();
                    
                    // Delete the file if it exists
                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        _fileStorageService.DeleteFile(photoPath);
                    }
                }
                
                TempData["Message"] = "Gallery image deleted successfully.";
                return RedirectToAction(nameof(ManageGallery), new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gallery image ID {ImageId}", imageId);
                TempData["Error"] = "An error occurred while deleting the image.";
                return RedirectToAction(nameof(ManageGallery), new { id = restaurantId });
            }
        }
    }
}