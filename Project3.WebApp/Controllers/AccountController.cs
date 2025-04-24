using Microsoft.AspNetCore.Mvc;
using Project3.WebApp.Repositories;
using Project3.WebApp.Services;
using System.Security.Claims;

namespace Project3.WebApp.Controllers
{
    /// <summary>
    /// Controller responsible for handling user account actions like
    /// Login, Logout, Registration, Password Reset, etc.
    /// </summary>
    [Route("[controller]")]
    public partial class AccountController : Controller
    {
        // Dependencies
        private readonly ILogger<AccountController> _logger;
        private readonly UserRepository _userRepository;
        private readonly AuthService _authService;

        /// <summary>
        /// Constructor to initialize the controller with required services.
        /// </summary>
        public AccountController(
            ILogger<AccountController> logger,
            UserRepository userRepository,
            AuthService authService)
        {
            _logger = logger;
            _userRepository = userRepository;
            _authService = authService;
        }

        /// <summary>
        /// Determines the correct dashboard action based on the user's role.
        /// </summary>
        /// <returns>An IActionResult redirecting to the appropriate controller/action.</returns>
        private IActionResult RedirectToDashboard()
        {
            // Get the user role from claims
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            string userRole = roleClaim?.Value?.ToLowerInvariant() ?? string.Empty;
            
            // Check roles using case-insensitive comparison
            bool isRestaurantRep = userRole.Equals("restaurantrep", StringComparison.OrdinalIgnoreCase);
            bool isReviewer = userRole.Equals("reviewer", StringComparison.OrdinalIgnoreCase);
            
            // Redirect based on role
            if (isRestaurantRep)
            {
                return RedirectToAction("Index", "RestaurantRepHome");
            }
            else if (isReviewer)
            {
                return RedirectToAction("Index", "ReviewerHome");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
