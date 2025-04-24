using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
// Add any other necessary using statements for your ViewModels or services
using Project3.Shared.Models.ViewModels; // For ReviewerHomeViewModel and ReviewViewModel
using Project3.Shared.Utilities; // For Connection class
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims; // Needed for getting user ID potentially
// using System.Linq; // If using LINQ for data retrieval

namespace Project3.WebApp.Controllers
{
    // Remove the role requirement from the Authorize attribute at the class level
    // and handle it in each method individually
    public class ReviewerHomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Connection _dbConnect;
        private readonly ILogger<ReviewerHomeController> _logger;

        // --- Constructor ---
        public ReviewerHomeController(IConfiguration configuration, Connection dbConnect, ILogger<ReviewerHomeController> logger)
        {
            _configuration = configuration;
            _dbConnect = dbConnect;
            _logger = logger;
        }

        // --- Index Action ---
        public async Task<IActionResult> Index()
        {
            // Log all claims for debugging
            _logger.LogInformation("Reviewer Home Index - User claims:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            
            // Check if user is authenticated first
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated, redirecting to login");
                return RedirectToAction("Login", "Account");
            }
            
            // Get role from claims instead of using IsInRole
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (roleClaim == null)
            {
                _logger.LogWarning("User has no role claim, redirecting to AccessDenied");
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Check for Reviewer role using case-insensitive comparison
            string userRole = roleClaim.Value;
            bool isReviewer = string.Equals(userRole, "Reviewer", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("Reviewer Home Index - User Role: {Role}, IsReviewer: {IsReviewer}", 
                userRole, isReviewer);
            
            if (!isReviewer)
            {
                _logger.LogWarning("User with role {Role} attempted to access ReviewerHome but is not a Reviewer", userRole);
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Get current user ID
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                // If the user doesn't exist in the database, sign them out
                if (User?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogWarning("User is authenticated but not found in database, signing out");
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["Message"] = "Your account was not found. Please sign in again.";
                }
                else
                {
                    TempData["ErrorMessage"] = "You must be logged in to view your dashboard.";
                }
                return RedirectToAction("Login", "Account");
            }

            // Create the view model
            var viewModel = new ReviewerHomeViewModel
            {
                WelcomeMessage = $"Welcome, {User.Identity?.Name ?? "Reviewer"}!"
            };

            try
            {
                // Query to get review count for the current user
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.*, rst.Name AS RestaurantName
                    FROM TP_Reviews r
                    INNER JOIN TP_Restaurants rst ON r.RestaurantID = rst.RestaurantID
                    WHERE r.UserID = @UserID
                    ORDER BY r.CreatedDate DESC");
                cmd.Parameters.AddWithValue("@UserID", currentUserId);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                if (ds != null && ds.Tables.Count > 0)
                {
                    viewModel.TotalReviewsCount = ds.Tables[0].Rows.Count;
                }
                else
                {
                    viewModel.TotalReviewsCount = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for ReviewerHome for User ID {UserId}", currentUserId);
                viewModel.TotalReviewsCount = 0;
            }

            return View(viewModel);
        }

        // --- ManageReviews Action ---
        // This action will handle GET requests to /ReviewerHome/ManageReviews
        [HttpGet] // Explicitly marking as HttpGet (optional if no other verb is present, but clear)
        public async Task<IActionResult> ManageReviews()
        {
            // Log all claims for debugging
            _logger.LogInformation("ManageReviews - User claims:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            
            // Check if user is authenticated first
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated, redirecting to login");
                return RedirectToAction("Login", "Account");
            }
            
            // Get role from claims instead of using IsInRole
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (roleClaim == null)
            {
                _logger.LogWarning("User has no role claim, redirecting to AccessDenied");
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Check for Reviewer role using case-insensitive comparison
            string userRole = roleClaim.Value;
            bool isReviewer = string.Equals(userRole, "Reviewer", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("ManageReviews - User Role: {Role}, IsReviewer: {IsReviewer}", 
                userRole, isReviewer);
            
            if (!isReviewer)
            {
                _logger.LogWarning("User with role {Role} attempted to access ManageReviews but is not a Reviewer", userRole);
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Get the current user ID
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                // If the user doesn't exist in the database, sign them out
                if (User?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogWarning("User is authenticated but not found in database, signing out");
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    TempData["Message"] = "Your account was not found. Please sign in again.";
                }
                else
                {
                    TempData["ErrorMessage"] = "You must be logged in to view your reviews.";
                }
                return RedirectToAction("Login", "Account");
            }

            try
            {
                _logger.LogInformation("Fetching reviews for user ID: {UserId}", currentUserId);
                
                // Query to get all reviews created by the current user with restaurant details
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.*, rst.Name AS RestaurantName
                    FROM TP_Reviews r
                    INNER JOIN TP_Restaurants rst ON r.RestaurantID = rst.RestaurantID
                    WHERE r.UserID = @UserID
                    ORDER BY r.CreatedDate DESC");
                cmd.Parameters.AddWithValue("@UserID", currentUserId);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                // Create list of reviews for the view
                var reviews = new List<ReviewViewModel>();
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        reviews.Add(new ReviewViewModel
                        {
                            ReviewID = Convert.ToInt32(row["ReviewID"]),
                            RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                            RestaurantName = row["RestaurantName"].ToString(),
                            UserID = currentUserId,
                            VisitDate = Convert.ToDateTime(row["VisitDate"]),
                            Comments = row["Comments"].ToString(),
                            FoodQualityRating = Convert.ToInt32(row["FoodQualityRating"]),
                            ServiceRating = Convert.ToInt32(row["ServiceRating"]),
                            AtmosphereRating = Convert.ToInt32(row["AtmosphereRating"]),
                            PriceRating = Convert.ToInt32(row["PriceRating"]),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            ReviewerUsername = User.Identity.Name
                        });
                    }
                    
                    _logger.LogInformation("Found {Count} reviews for user ID {UserId}", reviews.Count, currentUserId);
                }
                else
                {
                    _logger.LogInformation("No reviews found for user ID {UserId}", currentUserId);
                }

                return View(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews for user {UserId}", currentUserId);
                TempData["ErrorMessage"] = "There was an error retrieving your reviews. Please try again.";
                return View(new List<ReviewViewModel>());
            }
        }

        // Helper method to get the current user's ID
        private int GetCurrentUserId()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                try
                {
                    // Verify user exists in the database
                    SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM TP_Users WHERE UserID = @UserID");
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    var result = _dbConnect.ExecuteScalarUsingCmdObj(cmd);
                    
                    if (result != null && Convert.ToInt32(result) > 0)
                    {
                        return userId; // User exists
                    }
                    else
                    {
                        _logger.LogWarning("User with ID {UserId} not found in database", userId);
                        return 0; // User doesn't exist
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying user existence for ID {UserId}", userId);
                    return userId; // Return the ID anyway on error
                }
            }

            return 0;
        }

        // Add other actions needed for the Reviewer Home section here...

    }
}
