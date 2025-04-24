using Microsoft.AspNetCore.Authorization; // Needed for [Authorize]
using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.ViewModels; // For ReviewViewModel
using Project3.Shared.Utilities;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
// Add other necessary using statements (Models, DB access, etc.)

namespace Project3.Controllers
{
    [Authorize(Roles = "Reviewer")] // Only reviewers can create reviews
    public class ReviewController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly Connection _dbConnect;
        private readonly ILogger<ReviewController> _logger;

        // --- Constructor ---
        public ReviewController(IConfiguration configuration, Connection dbConnect, ILogger<ReviewController> logger)
        {
            _configuration = configuration;
            _dbConnect = dbConnect;
            _logger = logger;
        }

        // --- GET: /Review/Create?restaurantId=123 ---
        // Shows the form to create a new review
        [HttpGet]
        public IActionResult Create(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Get restaurant details to display in the form
                SqlCommand cmd = new SqlCommand("SELECT Name FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                string restaurantName = "Restaurant";
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    restaurantName = ds.Tables[0].Rows[0]["Name"].ToString();
                }

                // Create view model
                var model = new ReviewViewModel
                {
                    RestaurantID = restaurantId,
                    RestaurantName = restaurantName,
                    VisitDate = DateTime.Today, // Default to today
                    UserID = GetCurrentUserId(),
                    ReviewerUsername = User.Identity?.Name ?? "Unknown"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading restaurant details for review creation: {RestaurantId}", restaurantId);
                TempData["ErrorMessage"] = "Could not load restaurant details. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // --- POST: /Review/Create ---
        // Processes the submitted review form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ReviewViewModel model)
        {
            // Debug information
            var debugInfo = $"Received: RestaurantID={model.RestaurantID}, " +
                            $"VisitDate={model.VisitDate}, " +
                            $"FoodRating={model.FoodQualityRating}, " +
                            $"ServiceRating={model.ServiceRating}, " +
                            $"AtmosphereRating={model.AtmosphereRating}, " +
                            $"PriceRating={model.PriceRating}, " +
                            $"Comments={model.Comments?.Substring(0, Math.Min(20, model.Comments?.Length ?? 0))}";
            _logger.LogInformation(debugInfo);

            if (!ModelState.IsValid)
            {
                // Get restaurant name again for the view if validation fails
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT Name FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                    cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                    var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        model.RestaurantName = ds.Tables[0].Rows[0]["Name"].ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving restaurant name on validation failure");
                }

                // Add debug information to ViewBag
                ViewBag.DebugInfo = "Model validation failed: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return View(model);
            }

            try
            {
                // Set current user ID
                model.UserID = GetCurrentUserId();
                
                if (model.UserID <= 0)
                {
                    ModelState.AddModelError("", "User not authenticated or user ID not found");
                    ViewBag.DebugInfo = $"User ID issue: {model.UserID}";
                    return View(model);
                }

                // Ensure ReviewerUsername is set
                if (string.IsNullOrEmpty(model.ReviewerUsername))
                {
                    model.ReviewerUsername = User.Identity?.Name ?? "Unknown";
                }

                // Log the values being sent to DB
                _logger.LogInformation("Saving review: Restaurant={0}, User={1}, Food={2}, Service={3}, Atmosphere={4}, Price={5}",
                    model.RestaurantID, model.UserID, model.FoodQualityRating, model.ServiceRating, model.AtmosphereRating, model.PriceRating);

                // Call stored procedure to add the review
                SqlCommand cmd = new SqlCommand("TP_spAddReview");
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                cmd.Parameters.AddWithValue("@UserID", model.UserID);
                cmd.Parameters.AddWithValue("@VisitDate", model.VisitDate);
                cmd.Parameters.AddWithValue("@Comments", model.Comments ?? string.Empty);
                cmd.Parameters.AddWithValue("@FoodQualityRating", model.FoodQualityRating);
                cmd.Parameters.AddWithValue("@ServiceRating", model.ServiceRating);
                cmd.Parameters.AddWithValue("@AtmosphereRating", model.AtmosphereRating);
                cmd.Parameters.AddWithValue("@PriceRating", model.PriceRating);

                // Add debug logging for parameters
                _logger.LogInformation("SQL Parameters: " +
                    $"@RestaurantID={model.RestaurantID}, " +
                    $"@UserID={model.UserID}, " +
                    $"@VisitDate={model.VisitDate}, " +
                    $"@Comments={model.Comments?.Substring(0, Math.Min(20, model.Comments?.Length ?? 0))}, " +
                    $"@FoodQualityRating={model.FoodQualityRating}, " +
                    $"@ServiceRating={model.ServiceRating}, " +
                    $"@AtmosphereRating={model.AtmosphereRating}, " +
                    $"@PriceRating={model.PriceRating}");

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                
                _logger.LogInformation("DoUpdateUsingCmdObj result: {0}", result);

                if (result > 0)
                {
                    _logger.LogInformation("Review created successfully for restaurant {RestaurantId} by user {UserId}", 
                        model.RestaurantID, model.UserID);
                    TempData["SuccessMessage"] = "Your review has been submitted successfully!";

                    // Redirect to the restaurant details page to see the new review
                    return RedirectToAction("Details", "Restaurant", new { id = model.RestaurantID });
                }
                else
                {
                    ViewBag.DebugInfo = "Database operation returned 0 rows affected. Review may not have been saved. Result: " + result;
                    
                    // Check if the review was actually saved despite reporting 0 rows affected
                    try
                    {
                        SqlCommand checkCmd = new SqlCommand(@"
                            SELECT TOP 1 ReviewID FROM TP_Reviews 
                            WHERE RestaurantID = @RestaurantID AND UserID = @UserID
                            ORDER BY CreatedDate DESC");
                        checkCmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                        checkCmd.Parameters.AddWithValue("@UserID", model.UserID);
                        
                        var ds = _dbConnect.GetDataSetUsingCmdObj(checkCmd);
                        
                        // If we find a review, it means the review was actually saved successfully
                        if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            _logger.LogInformation("Review found in database despite 0 rows affected result");
                            TempData["SuccessMessage"] = "Your review has been submitted successfully!";
                            return RedirectToAction("Details", "Restaurant", new { id = model.RestaurantID });
                        }
                    }
                    catch (Exception checkEx)
                    {
                        _logger.LogError(checkEx, "Error checking if review was actually saved");
                    }
                    
                    // If we reach here, the review was truly not saved
                    ModelState.AddModelError("", "Failed to submit your review. The database operation did not complete successfully.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for restaurant {RestaurantId}", model.RestaurantID);
                ViewBag.DebugInfo = $"Exception: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ViewBag.DebugInfo += $" Inner exception: {ex.InnerException.Message}";
                }
                ModelState.AddModelError("", "An error occurred while submitting your review. Please try again.");
                return View(model);
            }
        }

        // --- GET: /Review/Edit/5 ---
        // Shows the form to edit a review
        [HttpGet]
        public IActionResult Edit(int id) // id should match asp-route-id from the view link
        {
            if (id <= 0)
            {
                return RedirectToAction("ManageReviews", "ReviewerHome");
            }

            try
            {
                // Get the current user ID
                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit a review.";
                    return RedirectToAction("Login", "Account");
                }

                // Fetch the review with the given ID that belongs to the current user
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.*, rst.Name AS RestaurantName
                    FROM TP_Reviews r
                    INNER JOIN TP_Restaurants rst ON r.RestaurantID = rst.RestaurantID
                    WHERE r.ReviewID = @ReviewID AND r.UserID = @UserID");
                cmd.Parameters.AddWithValue("@ReviewID", id);
                cmd.Parameters.AddWithValue("@UserID", currentUserId);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                // Check if the review exists and belongs to the current user
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Review not found or you don't have permission to edit it.";
                    return RedirectToAction("ManageReviews", "ReviewerHome");
                }

                // Create view model from the data
                var row = ds.Tables[0].Rows[0];
                var model = new ReviewViewModel
                {
                    ReviewID = id,
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    RestaurantName = row["RestaurantName"].ToString(),
                    UserID = currentUserId,
                    VisitDate = Convert.ToDateTime(row["VisitDate"]),
                    Comments = row["Comments"].ToString(),
                    FoodQualityRating = Convert.ToInt32(row["FoodQualityRating"]),
                    ServiceRating = Convert.ToInt32(row["ServiceRating"]),
                    AtmosphereRating = Convert.ToInt32(row["AtmosphereRating"]),
                    PriceRating = Convert.ToInt32(row["PriceRating"]),
                    ReviewerUsername = User.Identity.Name
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review for editing: {ReviewId}", id);
                TempData["ErrorMessage"] = "Could not load review details. Please try again.";
                return RedirectToAction("ManageReviews", "ReviewerHome");
            }
        }

        // --- POST: /Review/Edit/5 ---
        // Processes the submitted edit form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ReviewViewModel model)
        {
            if (id != model.ReviewID)
            {
                return BadRequest("Review ID mismatch.");
            }

            // Verify the current user is authorized to edit this review
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0 || currentUserId != model.UserID)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this review.";
                return RedirectToAction("ManageReviews", "ReviewerHome");
            }

            // Debug log to see what values are being received
            _logger.LogInformation("Form values received - Food: {0}, Service: {1}, Atmosphere: {2}, Price: {3}",
                model.FoodQualityRating, model.ServiceRating, model.AtmosphereRating, model.PriceRating);
                
            // Debug: Log raw form data
            _logger.LogInformation("Raw Form Data:");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Key: {0}, Value: {1}", key, Request.Form[key]);
            }

            // DIRECT APPROACH: Extract star ratings directly from form
            // Check if there are star ratings in the form data
            if (Request.Form.ContainsKey("food_stars") || Request.Form.ContainsKey("service_stars") || 
                Request.Form.ContainsKey("atmosphere_stars") || Request.Form.ContainsKey("price_stars"))
            {
                _logger.LogInformation("Found direct star ratings in form data - using these values");
                
                // Extract star ratings directly from form input
                if (Request.Form.ContainsKey("food_stars") && !string.IsNullOrEmpty(Request.Form["food_stars"]))
                {
                    int.TryParse(Request.Form["food_stars"], out int foodRating);
                    if (foodRating >= 1 && foodRating <= 5)
                    {
                        model.FoodQualityRating = foodRating;
                        _logger.LogInformation("Overriding Food Rating with direct form value: {0}", foodRating);
                    }
                }
                
                if (Request.Form.ContainsKey("service_stars") && !string.IsNullOrEmpty(Request.Form["service_stars"]))
                {
                    int.TryParse(Request.Form["service_stars"], out int serviceRating);
                    if (serviceRating >= 1 && serviceRating <= 5)
                    {
                        model.ServiceRating = serviceRating;
                        _logger.LogInformation("Overriding Service Rating with direct form value: {0}", serviceRating);
                    }
                }
                
                if (Request.Form.ContainsKey("atmosphere_stars") && !string.IsNullOrEmpty(Request.Form["atmosphere_stars"]))
                {
                    int.TryParse(Request.Form["atmosphere_stars"], out int atmosphereRating);
                    if (atmosphereRating >= 1 && atmosphereRating <= 5)
                    {
                        model.AtmosphereRating = atmosphereRating;
                        _logger.LogInformation("Overriding Atmosphere Rating with direct form value: {0}", atmosphereRating);
                    }
                }
                
                if (Request.Form.ContainsKey("price_stars") && !string.IsNullOrEmpty(Request.Form["price_stars"]))
                {
                    int.TryParse(Request.Form["price_stars"], out int priceRating);
                    if (priceRating >= 1 && priceRating <= 5)
                    {
                        model.PriceRating = priceRating;
                        _logger.LogInformation("Overriding Price Rating with direct form value: {0}", priceRating);
                    }
                }
                
                // Log updated model values
                _logger.LogInformation("Updated model ratings - Food: {0}, Service: {1}, Atmosphere: {2}, Price: {3}",
                    model.FoodQualityRating, model.ServiceRating, model.AtmosphereRating, model.PriceRating);
            }

            if (!ModelState.IsValid)
            {
                // If validation fails, repopulate the model with restaurant name for the view
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT Name FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                    cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                    var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        model.RestaurantName = ds.Tables[0].Rows[0]["Name"].ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving restaurant name on validation failure");
                }

                return View(model);
            }

            try
            {
                // Update the review in the database
                SqlCommand cmd = new SqlCommand("TP_spUpdateReview");
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReviewID", model.ReviewID);
                cmd.Parameters.AddWithValue("@UserID", model.UserID);
                cmd.Parameters.AddWithValue("@VisitDate", model.VisitDate);
                cmd.Parameters.AddWithValue("@Comments", model.Comments ?? string.Empty);
                
                // Ensure integer values for ratings
                cmd.Parameters.AddWithValue("@FoodQualityRating", Convert.ToInt32(model.FoodQualityRating));
                cmd.Parameters.AddWithValue("@ServiceRating", Convert.ToInt32(model.ServiceRating));
                cmd.Parameters.AddWithValue("@AtmosphereRating", Convert.ToInt32(model.AtmosphereRating));
                cmd.Parameters.AddWithValue("@PriceRating", Convert.ToInt32(model.PriceRating));

                _logger.LogInformation("Updating review {ReviewId} with parameters: Food={0}, Service={1}, Atmosphere={2}, Price={3}",
                    model.ReviewID, model.FoodQualityRating, model.ServiceRating, model.AtmosphereRating, model.PriceRating);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                _logger.LogInformation("DoUpdateUsingCmdObj result: {0}", result);

                if (result > 0)
                {
                    // Force reload of data to clear any browser/client caching
                    TempData["ForceRefresh"] = true;
                    
                    _logger.LogInformation("Review updated successfully: {ReviewId}", model.ReviewID);
                    TempData["SuccessMessage"] = "Your review has been updated successfully!";
                    
                    // Force cache clearing by redirecting with a timestamp parameter
                    return RedirectToAction("Details", "Restaurant", new { 
                        id = model.RestaurantID, 
                        t = DateTime.Now.Ticks 
                    });
                }
                else
                {
                    // If no stored procedure exists, attempt a direct update
                    cmd = new SqlCommand(@"
                        UPDATE TP_Reviews WITH (ROWLOCK)
                        SET VisitDate = @VisitDate,
                            Comments = @Comments,
                            FoodQualityRating = @FoodQualityRating,
                            ServiceRating = @ServiceRating,
                            AtmosphereRating = @AtmosphereRating,
                            PriceRating = @PriceRating
                        WHERE ReviewID = @ReviewID AND UserID = @UserID");

                    cmd.Parameters.AddWithValue("@ReviewID", model.ReviewID);
                    cmd.Parameters.AddWithValue("@UserID", model.UserID);
                    cmd.Parameters.AddWithValue("@VisitDate", model.VisitDate);
                    cmd.Parameters.AddWithValue("@Comments", model.Comments ?? string.Empty);
                    
                    // Ensure integer values for ratings
                    cmd.Parameters.AddWithValue("@FoodQualityRating", Convert.ToInt32(model.FoodQualityRating));
                    cmd.Parameters.AddWithValue("@ServiceRating", Convert.ToInt32(model.ServiceRating));
                    cmd.Parameters.AddWithValue("@AtmosphereRating", Convert.ToInt32(model.AtmosphereRating));
                    cmd.Parameters.AddWithValue("@PriceRating", Convert.ToInt32(model.PriceRating));

                    result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                    _logger.LogInformation("Direct SQL DoUpdateUsingCmdObj result: {0}", result);

                    if (result > 0)
                    {
                        // Force browser cache reset
                        TempData["ForceRefresh"] = true;
                        
                        _logger.LogInformation("Review updated successfully using direct SQL: {ReviewId}", model.ReviewID);
                        TempData["SuccessMessage"] = "Your review has been updated successfully!";
                        
                        // Force cache clearing by redirecting with a timestamp parameter
                        return RedirectToAction("Details", "Restaurant", new { 
                            id = model.RestaurantID, 
                            t = DateTime.Now.Ticks 
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to update your review. The database operation did not complete successfully.");
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", model.ReviewID);
                ModelState.AddModelError("", "An error occurred while updating your review. Please try again.");
                return View(model);
            }
        }

        // --- GET: /Review/Delete/5 ---
        // Shows a confirmation page before deleting
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid review ID.");
            }

            // Get the current user ID for authorization check
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                TempData["ErrorMessage"] = "You must be logged in to delete a review.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Fetch the review with restaurant details
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.*, rst.Name AS RestaurantName
                    FROM TP_Reviews r
                    INNER JOIN TP_Restaurants rst ON r.RestaurantID = rst.RestaurantID
                    WHERE r.ReviewID = @ReviewID AND r.UserID = @UserID");
                cmd.Parameters.AddWithValue("@ReviewID", id);
                cmd.Parameters.AddWithValue("@UserID", currentUserId);

                var ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                // Check if the review exists and belongs to the current user
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Review not found or you don't have permission to delete it.";
                    return RedirectToAction("ManageReviews", "ReviewerHome");
                }

                // Create view model from the data
                var row = ds.Tables[0].Rows[0];
                var model = new ReviewViewModel
                {
                    ReviewID = id,
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    RestaurantName = row["RestaurantName"].ToString(),
                    UserID = currentUserId,
                    VisitDate = Convert.ToDateTime(row["VisitDate"]),
                    Comments = row["Comments"].ToString(),
                    FoodQualityRating = Convert.ToInt32(row["FoodQualityRating"]),
                    ServiceRating = Convert.ToInt32(row["ServiceRating"]),
                    AtmosphereRating = Convert.ToInt32(row["AtmosphereRating"]),
                    PriceRating = Convert.ToInt32(row["PriceRating"]),
                    ReviewerUsername = User.Identity.Name
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review for deletion: {ReviewId}", id);
                TempData["ErrorMessage"] = "Could not load review details. Please try again.";
                return RedirectToAction("ManageReviews", "ReviewerHome");
            }
        }
        
        // --- POST: /Review/DeleteConfirmed/5 ---
        // Performs the actual deletion
        [HttpPost]
        [ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _logger.LogInformation("DeleteConfirmed called with id: {0}", id);
            
            // Get the current user ID for authorization check
            var currentUserId = GetCurrentUserId();
            if (currentUserId <= 0)
            {
                TempData["ErrorMessage"] = "You must be logged in to delete a review.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // First get the review details to check authorization and get restaurant ID for redirect
                SqlCommand getCmd = new SqlCommand(@"
                    SELECT RestaurantID FROM TP_Reviews 
                    WHERE ReviewID = @ReviewID AND UserID = @UserID");
                getCmd.Parameters.AddWithValue("@ReviewID", id);
                getCmd.Parameters.AddWithValue("@UserID", currentUserId);

                var ds = _dbConnect.GetDataSetUsingCmdObj(getCmd);
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Review not found or you don't have permission to delete it.";
                    return RedirectToAction("ManageReviews", "ReviewerHome");
                }

                // Get restaurant ID for redirect after deletion
                int restaurantId = Convert.ToInt32(ds.Tables[0].Rows[0]["RestaurantID"]);

                // Delete the review using stored procedure if available
                try
                {
                    SqlCommand cmd = new SqlCommand("TP_spDeleteReview");
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ReviewID", id);
                    cmd.Parameters.AddWithValue("@UserID", currentUserId); // For authorization check

                    _logger.LogInformation("Executing TP_spDeleteReview for ReviewID: {0}, UserID: {1}", id, currentUserId);
                    int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                    _logger.LogInformation("Stored procedure delete result: {0}", result);

                    if (result > 0)
                    {
                        _logger.LogInformation("Review deleted successfully: {ReviewId}", id);
                        TempData["SuccessMessage"] = "Your review has been deleted successfully.";
                        return RedirectToAction("Details", "Restaurant", new { id = restaurantId });
                    }
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Exception during stored procedure delete for review {ReviewId}: {Message}", id, sqlEx.Message);
                    // Continue to direct SQL approach
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during stored procedure delete for review {ReviewId}: {Message}", id, ex.Message);
                    // Continue to direct SQL approach
                }

                // If stored procedure doesn't exist or fails, try direct SQL
                try
                {
                    _logger.LogInformation("Falling back to direct SQL delete");
                    
                    // Create a new command object to avoid any issues with the previous one
                    SqlCommand directCmd = new SqlCommand();
                    directCmd.CommandText = @"
                        DELETE FROM TP_Reviews 
                        WHERE ReviewID = @ReviewID AND UserID = @UserID";
                    directCmd.Parameters.AddWithValue("@ReviewID", id);
                    directCmd.Parameters.AddWithValue("@UserID", currentUserId);

                    int result = _dbConnect.DoUpdateUsingCmdObj(directCmd);
                    _logger.LogInformation("Direct SQL delete result: {0}", result);

                    if (result > 0)
                    {
                        _logger.LogInformation("Review deleted successfully using direct SQL: {ReviewId}", id);
                        TempData["SuccessMessage"] = "Your review has been deleted successfully.";
                        return RedirectToAction("Details", "Restaurant", new { id = restaurantId });
                    }
                    else
                    {
                        _logger.LogWarning("Delete failed - no rows affected");
                        TempData["ErrorMessage"] = "Failed to delete the review. Please try again.";
                    }
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Exception during direct SQL delete for review {ReviewId}: {Message}", id, sqlEx.Message);
                    TempData["ErrorMessage"] = $"Database error: {sqlEx.Message}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during direct SQL delete for review {ReviewId}: {Message}", id, ex.Message);
                    TempData["ErrorMessage"] = "An error occurred while deleting your review. Please try again.";
                }

                // Double check if the review still exists before showing error
                try 
                {
                    SqlCommand checkCmd = new SqlCommand("SELECT 1 FROM TP_Reviews WHERE ReviewID = @ReviewID");
                    checkCmd.Parameters.AddWithValue("@ReviewID", id);
                    var checkResult = _dbConnect.GetDataSetUsingCmdObj(checkCmd);
                    
                    if (checkResult == null || checkResult.Tables.Count == 0 || checkResult.Tables[0].Rows.Count == 0)
                    {
                        // Review no longer exists, it was deleted successfully
                        TempData["SuccessMessage"] = "Your review has been deleted successfully.";
                        TempData.Remove("ErrorMessage"); // Remove any error message
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking if review still exists after delete attempt: {ReviewId}", id);
                }

                // Redirect to restaurant details page
                return RedirectToAction("Details", "Restaurant", new { id = restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting your review. Please try again.";
                return RedirectToAction("ManageReviews", "ReviewerHome");
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
                return userId;
            }

            return 0;
        }
    }
}
