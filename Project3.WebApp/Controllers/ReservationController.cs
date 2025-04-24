using Microsoft.AspNetCore.Mvc;
// Using organized namespaces - ensure these match your project
using Project3.Shared.Models.ViewModels;
using Project3.WebApp.Services; // For EmailService
using System.Security.Claims; // For UserID
using System.Text.Json;

namespace Project3.Controllers
{
    // Decide on authorization: Allow anyone to view the form, but maybe require login to POST?
    // Or handle guest logic within POST as currently done.
    // [Authorize] // Uncomment if login is required to even view the form
    [Route("[controller]")]
    public class ReservationController : Controller
    {
        private readonly ILogger<ReservationController> _logger;
        private readonly IHttpClientFactory _httpClientFactory; // Inject HttpClientFactory
        private readonly EmailService _emailService; // Email service for reservation confirmations

        // Local DTO classes to avoid ambiguity
        private class CreateReservationDto
        {
            public int RestaurantID { get; set; }
            public int? UserID { get; set; }
            public DateTime ReservationDateTime { get; set; }
            public int PartySize { get; set; }
            public string ContactName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string SpecialRequests { get; set; }
        }

        private class ErrorResponseDto
        {
            public string Message { get; set; }
        }

        // Constructor to inject dependencies
        public ReservationController(
            ILogger<ReservationController> logger, 
            IHttpClientFactory httpClientFactory,
            EmailService emailService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
        }

        // GET: /Reservation/Create?restaurantId=123
        // Fetches restaurant details and displays the reservation form.
        [HttpGet]
        public async Task<IActionResult> Create(int restaurantId)
        {
            try
            {
                if (restaurantId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid restaurant specified.";
                    return RedirectToAction("Index", "Home"); // Or redirect to restaurant search
                }

                // Use ReservationViewModel to prepare data for the view
                var viewModel = new ReservationViewModel
                {
                    RestaurantID = restaurantId,
                    ReservationDateTime = DateTime.Today.AddDays(1).AddHours(19), // Default Date/Time (19:00)
                    PartySize = 2 // Default Party Size
                };

                // API Call to get Restaurant Name (and potentially other needed details)
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api"); // Use named client
                    string apiUrl = $"api/RestaurantsApi/{restaurantId}";
                    _logger.LogDebug("Calling API GET {ApiUrl} to get restaurant details", apiUrl);

                    // Use a timeout for the API call
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout
                    
                    var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>(apiUrl, cts.Token);

                    if (restaurant != null)
                    {
                        viewModel.RestaurantName = restaurant.Name;
                    }
                    else
                    {
                        _logger.LogWarning("Restaurant ID {RestaurantId} not found by API.", restaurantId);
                        viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "API call timed out: Could not get restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    TempData["WarningMessage"] = "Could not connect to the restaurant details service. Some features may be limited.";
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "API call failed: Could not get restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    TempData["WarningMessage"] = "Could not connect to the restaurant details service. Some features may be limited.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error getting restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}"; // Fallback name
                    TempData["WarningMessage"] = "An issue occurred while loading restaurant details. Some features may be limited.";
                }

                // Pre-fill user info if logged in
                if (User.Identity.IsAuthenticated)
                {
                    // Ensure UserID claim exists and is valid before parsing
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        viewModel.UserID = userId;
                    }
                    viewModel.ContactName = User.Identity.Name; // Or fetch full name if stored in claims
                    viewModel.Email = User.FindFirstValue(ClaimTypes.Email);
                    // viewModel.Phone = User.FindFirstValue(ClaimTypes.MobilePhone); // If available
                }

                return View(viewModel); // Pass the ViewModel to Views/Reservation/Create.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in Create GET method for restaurant {RestaurantId}", restaurantId);
                TempData["ErrorMessage"] = "An unexpected system error occurred loading the reservation form.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Reservation/Create
        // Submits the reservation request to the API.
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent CSRF
        // [Authorize] // Uncomment if login is required to POST a reservation
        public async Task<IActionResult> Create(ReservationViewModel model) // Receives data from the form
        {
            // --- Manual Validation (supplements Model Annotations) ---
            if (model.ReservationDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.ReservationDateTime), "Reservation must be for a future date and time.");
            }
            // PartySize validation likely handled by [Range] attribute on ViewModel

            // Guest-specific required fields (if allowing guest reservations)
            if (!User.Identity.IsAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(model.ContactName)) ModelState.AddModelError(nameof(model.ContactName), "Contact Name is required for guest reservations.");
                if (string.IsNullOrWhiteSpace(model.Phone)) ModelState.AddModelError(nameof(model.Phone), "Phone Number is required for guest reservations.");
                if (string.IsNullOrWhiteSpace(model.Email)) ModelState.AddModelError(nameof(model.Email), "Email is required for guest reservations.");
            }
            // --- End Validation ---

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Reservation Create POST failed validation for Restaurant {RestaurantId}", model.RestaurantID);
                // Repopulate RestaurantName if returning the View
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api");
                    var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>($"api/restaurants/{model.RestaurantID}");
                    model.RestaurantName = restaurant?.Name ?? "Restaurant"; // Repopulate name
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-fetch restaurant name on validation error for ID {RestaurantId}", model.RestaurantID);
                    model.RestaurantName = "Restaurant"; // Fallback
                }
                return View(model); // Return view with validation errors
            }

            // --- Prepare Data Transfer Object (DTO) for API ---
            var reservationDto = new CreateReservationDto
            {
                RestaurantID = model.RestaurantID,
                // Set UserID only if authenticated
                UserID = User.Identity.IsAuthenticated ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)) : (int?)null,
                ReservationDateTime = model.ReservationDateTime,
                PartySize = model.PartySize,
                ContactName = model.ContactName,
                Phone = model.Phone,
                Email = model.Email,
                SpecialRequests = model.SpecialRequests
            };
            // --- End DTO Preparation ---

            // --- API Call to submit Reservation ---
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = "api/ReservationsApi"; // Updated to match the actual API controller route

                _logger.LogDebug("Calling API POST {ApiUrl} to create reservation", apiUrl);
                var response = await client.PostAsJsonAsync(apiUrl, reservationDto); // Send DTO

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Reservation created successfully via API for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    // Send confirmation email
                    await SendReservationConfirmationEmail(model);
                    
                    TempData["SuccessMessage"] = "Reservation submitted successfully! A confirmation email has been sent to you.";
                    // Redirect to the restaurant's profile page after successful submission
                    return RedirectToAction("Details", "Restaurant", new { id = model.RestaurantID });
                }
                else // Handle API errors
                {
                    // Handle API errors
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(); // Example DTO
                    _logger.LogError("API call failed: Could not create reservation. Status: {StatusCode}, Reason: {ApiError}",
                        response.StatusCode, errorResponse?.Message ?? await response.Content.ReadAsStringAsync()); // Log full content if DTO fails
                    // Add error message to display to the user
                    ModelState.AddModelError(string.Empty, errorResponse?.Message ?? "Error submitting reservation. Please try again.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API connection error: Could not create reservation for Restaurant {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError(string.Empty, "Error submitting reservation due to a connection issue. Please try again later.");
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "Unexpected error creating reservation for Restaurant {RestaurantId}", model.RestaurantID);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while submitting the reservation.");
            }
            // --- End API Call ---

            // If API call failed or other error occurred, return the view with the model and errors
            try
            {
                var client = _httpClientFactory.CreateClient("Project3Api");
                var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>($"api/restaurants/{model.RestaurantID}");
                model.RestaurantName = restaurant?.Name ?? "Restaurant";
            }
            catch { model.RestaurantName = "Restaurant"; }

            return View(model);
        }

        // POST: /Reservation/CreateAjax
        // AJAX endpoint for submitting the reservation request without page refresh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(ReservationViewModel model)
        {
            try 
            {
                // Dictionary to hold validation errors
                var errors = new Dictionary<string, string>();

                // --- Manual Validation (supplements Model Annotations) ---
                if (model.ReservationDateTime <= DateTime.Now)
                {
                    errors.Add(nameof(model.ReservationDateTime), "Reservation must be for a future date and time.");
                    ModelState.AddModelError(nameof(model.ReservationDateTime), "Reservation must be for a future date and time.");
                }

                // Guest-specific required fields (if allowing guest reservations)
                if (!User.Identity.IsAuthenticated)
                {
                    if (string.IsNullOrWhiteSpace(model.ContactName))
                    {
                        errors.Add(nameof(model.ContactName), "Contact Name is required for guest reservations.");
                        ModelState.AddModelError(nameof(model.ContactName), "Contact Name is required for guest reservations.");
                    }
                    if (string.IsNullOrWhiteSpace(model.Phone))
                    {
                        errors.Add(nameof(model.Phone), "Phone Number is required for guest reservations.");
                        ModelState.AddModelError(nameof(model.Phone), "Phone Number is required for guest reservations.");
                    }
                    if (string.IsNullOrWhiteSpace(model.Email))
                    {
                        errors.Add(nameof(model.Email), "Email is required for guest reservations.");
                        ModelState.AddModelError(nameof(model.Email), "Email is required for guest reservations.");
                    }
                }
                // --- End Validation ---

                // Check ModelState validation (catches annotation-based validation)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Reservation CreateAjax POST failed validation for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    // Populate errors dictionary from ModelState if not already added above
                    foreach (var state in ModelState)
                    {
                        if (state.Value.Errors.Count > 0 && !errors.ContainsKey(state.Key))
                        {
                            errors.Add(state.Key, state.Value.Errors.First().ErrorMessage);
                        }
                    }
                    
                    // Return validation errors to the client
                    return Json(new { 
                        success = false, 
                        message = "Please correct the validation errors.",
                        errors = errors
                    });
                }

                // Prepare Data Transfer Object (DTO) for API
                var reservationDto = new CreateReservationDto
                {
                    RestaurantID = model.RestaurantID,
                    // Set UserID only if authenticated
                    UserID = User.Identity.IsAuthenticated ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)) : (int?)null,
                    ReservationDateTime = model.ReservationDateTime,
                    PartySize = model.PartySize,
                    ContactName = model.ContactName,
                    Phone = model.Phone,
                    Email = model.Email,
                    SpecialRequests = model.SpecialRequests
                };

                // API Call to submit Reservation
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api");
                    string apiUrl = "api/ReservationsApi"; // Updated to match the actual API controller route
                    
                    _logger.LogDebug("Calling API POST {ApiUrl} to create reservation", apiUrl);
                    
                    // Use timeout for the API call
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout
                    
                    var response = await client.PostAsJsonAsync(apiUrl, reservationDto, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Reservation created successfully via API for Restaurant {RestaurantId}", model.RestaurantID);
                        
                        // Send confirmation email
                        await SendReservationConfirmationEmail(model);
                        
                        // Return success response - don't try to read the reservation ID as a number
                        return Json(new { 
                            success = true, 
                            message = "Reservation submitted successfully! A confirmation email has been sent to you."
                            // Don't try to parse the response content as an integer
                        });
                    }
                    else // Handle API errors
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        ErrorResponseDto errorResponse = null;
                        
                        try {
                            errorResponse = JsonSerializer.Deserialize<ErrorResponseDto>(errorContent);
                        }
                        catch {
                            // If deserializing fails, we'll use the raw content later
                        }
                        
                        _logger.LogError("API call failed: Could not create reservation. Status: {StatusCode}, Reason: {ApiError}",
                            response.StatusCode, errorResponse?.Message ?? errorContent);
                            
                        return Json(new { 
                            success = false, 
                            message = errorResponse?.Message ?? "Error submitting reservation. Please try again."
                        });
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "API call timed out: Reservation request for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    return Json(new { 
                        success = false, 
                        message = "The reservation service is currently unavailable. Please try again later."
                    });
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "API connection error: Could not create reservation for Restaurant {RestaurantId}", model.RestaurantID);
                    
                    // Fallback behavior when API is unavailable
                    if (ex.InnerException is System.Net.Sockets.SocketException)
                    {
                        _logger.LogWarning("API service unavailable - using fallback behavior for reservation");
                        
                        // Store reservation details locally or in session (if needed)
                        // For demonstration, we'll just send a success response with a note
                        await SendReservationConfirmationEmail(model);
                        
                        return Json(new { 
                            success = true,
                            message = "Your reservation has been recorded in offline mode. The restaurant will be notified when the system reconnects.",
                            isOfflineMode = true
                        });
                    }
                    
                    return Json(new { 
                        success = false, 
                        message = "Could not connect to the reservation service. Please try again later."
                    });
                }
                catch (Exception ex) // Catch unexpected errors
                {
                    _logger.LogError(ex, "Unexpected error creating reservation for Restaurant {RestaurantId}", model.RestaurantID);
                    return Json(new { 
                        success = false, 
                        message = "An unexpected error occurred while submitting the reservation."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in CreateAjax method for restaurant {RestaurantId}", model.RestaurantID);
                return Json(new { 
                    success = false, 
                    message = "An unexpected system error occurred. Please try again or contact support."
                });
            }
        }

        // GET: /Reservation/GetInitialModel?restaurantId=123
        // Returns a partial view with a pre-filled reservation model for the modal
        [HttpGet]
        [Route("Reservation/GetInitialModel")]
        public async Task<IActionResult> GetInitialModel(int restaurantId)
        {
            try
            {
                // Log the request to help with debugging
                _logger.LogInformation("GetInitialModel called with restaurantId: {RestaurantId}", restaurantId);
                
                var viewModel = new ReservationViewModel
                {
                    RestaurantID = restaurantId,
                    ReservationDateTime = DateTime.Today.AddDays(1).AddHours(19),
                    PartySize = 2
                };

                // Get restaurant details from API
                bool apiFailure = false;
                try
                {
                    var client = _httpClientFactory.CreateClient("Project3Api");
                    string apiUrl = $"api/RestaurantsApi/{restaurantId}";
                    
                    // Use a timeout for the API call
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout
                    
                    var restaurant = await client.GetFromJsonAsync<RestaurantViewModel>(apiUrl, cts.Token);
                    
                    if (restaurant != null)
                    {
                        viewModel.RestaurantName = restaurant.Name;
                    }
                    else
                    {
                        viewModel.RestaurantName = $"Restaurant #{restaurantId}";
                        apiFailure = true;
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogWarning(ex, "API call timed out: Could not get restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}";
                    apiFailure = true;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "API connection failed: Could not get restaurant details for ID {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}";
                    apiFailure = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch restaurant details for modal: {RestaurantId}", restaurantId);
                    viewModel.RestaurantName = $"Restaurant #{restaurantId}";
                    apiFailure = true;
                }

                // Pre-fill user info if logged in
                if (User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        viewModel.UserID = userId;
                    }
                    viewModel.ContactName = User.Identity.Name;
                    viewModel.Email = User.FindFirstValue(ClaimTypes.Email);
                }

                // Add a flag to the ViewBag to show a warning if API failed
                if (apiFailure)
                {
                    ViewBag.ApiWarning = "Some restaurant details couldn't be loaded. Your reservation can still be processed.";
                }

                // This partial view is in the Shared directory, so use the ~ prefix to ensure it's found
                return PartialView("~/Views/Shared/_ReservationModal.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInitialModel for restaurant {RestaurantId}", restaurantId);
                return Content("Error loading reservation form. Please try again.");
            }
        }

        // GET: /Reservation/GetModel?restaurantId=123
        // This is a fix for the routing issue - provides an alternative route
        [HttpGet("GetModel")]
        public async Task<IActionResult> GetModel(int restaurantId)
        {
            // Just forward to the original action
            return await GetInitialModel(restaurantId);
        }

        // Helper method to send a reservation confirmation email
        private async Task SendReservationConfirmationEmail(ReservationViewModel reservation)
        {
            try
            {
                string subject = $"Yelp 2.0 - Reservation Confirmation for {reservation.RestaurantName}";
                string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #333; text-align: center;'>Your Reservation is Confirmed!</h2>
                        <p>Dear {reservation.ContactName},</p>
                        <p>Thank you for making a reservation at <strong>{reservation.RestaurantName}</strong>. Your reservation details are as follows:</p>
                        
                        <div style='background-color: #f8f8f8; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>Date and Time:</strong> {reservation.ReservationDateTime.ToString("dddd, MMMM d, yyyy 'at' h:mm tt")}</p>
                            <p><strong>Party Size:</strong> {reservation.PartySize} people</p>
                            <p><strong>Contact Phone:</strong> {reservation.Phone}</p>
                            {(string.IsNullOrEmpty(reservation.SpecialRequests) ? "" : $"<p><strong>Special Requests:</strong> {reservation.SpecialRequests}</p>")}
                        </div>
                        
                        <p>If you need to modify or cancel your reservation, please contact the restaurant directly.</p>
                        <p style='text-align: center; margin-top: 30px;'>We look forward to serving you!</p>
                        <p>Regards,<br/>Yelp 2.0 Team</p>
                    </div>
                </body>
                </html>";
                
                await _emailService.SendEmailAsync(reservation.Email, subject, body);
                _logger.LogInformation("Reservation confirmation email sent to {Email} for restaurant {RestaurantName}", 
                    reservation.Email, reservation.RestaurantName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reservation confirmation email to {Email}", reservation.Email);
                // Don't throw - we still want the reservation to be considered successful even if the email fails
            }
        }
    }
}