using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Utilities;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;

namespace Project3.WebApp.Controllers // Ensure namespace matches your project
{
    public class HomeController : Controller
    {
        // Logger is optional, you can keep it or remove it if not used
        private readonly ILogger<HomeController> _logger;
        private readonly Connection _dbConnect;

        public HomeController(ILogger<HomeController> logger, Connection dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        // Remove [Authorize] to make the home page accessible to everyone
        public async Task<IActionResult> Index()
        {
            // If user is not authenticated, redirect to login
            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogInformation("User is not authenticated, redirecting to login page");
                return RedirectToAction("Login", "Account");
            }
            
            // Verify user still exists in database
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                try
                {
                    // Check if user still exists in the database
                    SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM TP_Users WHERE UserID = @UserID");
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    var result = _dbConnect.ExecuteScalarUsingCmdObj(cmd);
                    bool userExists = result != null && Convert.ToInt32(result) > 0;
                    
                    if (!userExists)
                    {
                        _logger.LogWarning("User with ID {UserId} does not exist in the database. Signing out.", userId);
                        // User doesn't exist anymore, sign them out
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        TempData["Message"] = "Your account was not found. Please sign in again.";
                        return RedirectToAction("Login", "Account");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking if user {UserId} exists", userId);
                    // On error, proceed with normal operations
                }
            }
            
            // Log all claims for debugging
            _logger.LogInformation("User claims:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            
            // Get role from claims instead of using IsInRole
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (roleClaim == null)
            {
                _logger.LogWarning("User has no role claim, signing out and redirecting to login");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Your account has no role assigned. Please contact support.";
                return RedirectToAction("Login", "Account");
            }
            
            string userRole = roleClaim.Value.ToLowerInvariant();
            _logger.LogInformation($"User role from claims: {userRole}");
            
            // Check roles using string comparison instead of IsInRole
            bool isRestaurantRep = userRole.Equals("restaurantrep", StringComparison.OrdinalIgnoreCase);
            bool isReviewer = userRole.Equals("reviewer", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation($"User is in RestaurantRep role: {isRestaurantRep}");
            _logger.LogInformation($"User is in Reviewer role: {isReviewer}");
            
            // Redirect based on role
            if (isRestaurantRep)
            {
                _logger.LogInformation("Redirecting to RestaurantRepHome/Index");
                return RedirectToAction("Index", "RestaurantRepHome");
            }
            else if (isReviewer)
            {
                _logger.LogInformation("Redirecting to ReviewerHome/Index");
                return RedirectToAction("Index", "ReviewerHome");
            }
            else
            {
                _logger.LogWarning("User has unrecognized role, signing out and redirecting to login");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Your account role is not recognized. Please contact support.";
                return RedirectToAction("Login", "Account");
            }
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult SaveNotificationCounter(int count)
        {
            TempData["NotificationCount"] = count;
            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous] // <<< Optional: Allow anyone (even unauthenticated) to see the error page
        public IActionResult Error()
        {
            // Explicitly specifying ViewModels.ErrorViewModel to avoid ambiguity
            var errorViewModel = new Project3.Shared.Models.ViewModels.ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
            };
            return View(errorViewModel);
        }
    }
}
