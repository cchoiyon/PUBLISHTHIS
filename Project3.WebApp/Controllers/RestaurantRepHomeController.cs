using Microsoft.AspNetCore.Authorization; // Ensure controller requires login
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
// using Microsoft.Data.SqlClient; // Use this if Connection uses the newer package
using Project3.Shared.Models.Domain; // Add this to use the Reservation class
using Project3.Shared.Models.DTOs; // For ReviewDto etc.
using Project3.Shared.Models.ViewModels; // Needed for the ViewModel
using Project3.Shared.Services; // For FileStorageService
using Project3.Shared.Utilities; // For Connection
using System.Data;
// NOTE: Use EITHER System.Data.SqlClient OR Microsoft.Data.SqlClient, not both usually.
// Choose based on which package your Connection class uses.
using System.Data.SqlClient; // Assuming Connection uses this older version
using System.Security.Claims; // For identity
using System.Text.Json;


namespace Project3.Controllers
{
    [Authorize(Roles = "RestaurantRep")] // Restrict access to users with the RestaurantRep role
    public class RestaurantRepHomeController : Controller
    {
        private readonly Connection _db; // Example: Using Connection
        private readonly ILogger<RestaurantRepHomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FileStorageService _fileStorageService;
        private readonly IWebHostEnvironment _hostEnvironment;

        // Constructor for dependency injection
        public RestaurantRepHomeController(
            Connection db, 
            ILogger<RestaurantRepHomeController> logger, 
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment hostEnvironment,
            IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _hostEnvironment = hostEnvironment;
            _fileStorageService = new FileStorageService(hostEnvironment.ContentRootPath, configuration);
        }

        // GET: /RestaurantRepHome/Index
        // FIX: Changed method signature to async Task<IActionResult>
        public async Task<IActionResult> Index()
        {
            var viewModel = new RestaurantRepHomeViewModel();
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID
            string username = User.Identity?.Name ?? "Restaurant Rep"; // Get username for welcome message

            viewModel.WelcomeMessage = $"Welcome, {username}!";

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found for RestaurantRepHome Index.");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // --- Fetch Profile Status ---
                SqlCommand cmdProfile = new SqlCommand();
                cmdProfile.CommandText = @"
                    SELECT 
                        CASE WHEN EXISTS (
                            SELECT 1 FROM TP_Restaurants WHERE RestaurantID = @UserId
                        ) THEN 1 ELSE 0 END as HasProfile,
                        r.RestaurantID,
                        r.Name as RestaurantName
                    FROM TP_Restaurants r
                    WHERE r.RestaurantID = @UserId";
                cmdProfile.Parameters.AddWithValue("@UserId", userId);
                
                DataSet dsProfile = _db.GetDataSetUsingCmdObj(cmdProfile);

                if (dsProfile.Tables.Count > 0 && dsProfile.Tables[0].Rows.Count > 0)
                {
                    DataRow profileRow = dsProfile.Tables[0].Rows[0];
                    viewModel.HasProfile = Convert.ToBoolean(profileRow["HasProfile"]);
                    if (viewModel.HasProfile)
                    {
                        viewModel.RestaurantId = Convert.ToInt32(profileRow["RestaurantId"]);
                        viewModel.RestaurantName = profileRow["RestaurantName"]?.ToString();
                    }
                }
                else
                {
                    viewModel.HasProfile = false;
                }


                // --- Get Pending Reservation Count (only if profile exists) ---
                if (viewModel.HasProfile)
                {
                    try
                    {
                        // Use direct SQL query instead of a stored procedure
                        SqlCommand cmdReservations = new SqlCommand();
                        cmdReservations.CommandText = @"
                            SELECT COUNT(*) 
                            FROM TP_Reservations 
                            WHERE RestaurantID = @RestaurantId 
                            AND Status = 'Pending'";
                        cmdReservations.Parameters.AddWithValue("@RestaurantId", viewModel.RestaurantId);

                        // Execute the query
                        object reservationResult = _db.ExecuteScalarUsingCmdObj(cmdReservations);

                        if (reservationResult != null && reservationResult != DBNull.Value)
                        {
                            viewModel.PendingReservationCount = Convert.ToInt32(reservationResult);
                        }
                        else
                        {
                            viewModel.PendingReservationCount = 0; // Default if null or DBNull
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue - this is not critical functionality
                        _logger.LogWarning(ex, "Could not get pending reservation count. Error: {Message}", ex.Message);
                        viewModel.PendingReservationCount = 0; // Set default value
                    }
                }


                // --- Get Recent Reviews (only if profile exists) ---
                if (viewModel.HasProfile)
                {
                    try
                    {
                        SqlCommand cmdReviews = new SqlCommand();
                        cmdReviews.CommandType = CommandType.StoredProcedure;
                        cmdReviews.CommandText = "TP_spGetRestaurantReviews"; // Use the correct SP
                        cmdReviews.Parameters.AddWithValue("@RestaurantID", viewModel.RestaurantId);
                        
                        // Assuming GetDataSetUsingCmdObj is SYNCHRONOUS
                        DataSet dsReviews = _db.GetDataSetUsingCmdObj(cmdReviews);

                        if (dsReviews?.Tables.Count > 0 && dsReviews.Tables[0].Rows.Count > 0)
                        {
                            // Take only the latest 3 reviews if there are more
                            var reviewsToShow = dsReviews.Tables[0].AsEnumerable()
                                .OrderByDescending(r => r.Field<DateTime>("CreatedDate"))
                                .Take(3);
                                
                            foreach (DataRow row in reviewsToShow)
                            {
                                try
                                {
                                    // Log what columns are available
                                    _logger.LogInformation("Review columns: {Columns}", 
                                        string.Join(", ", row.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
                                    
                                    // Create the review DTO
                                    var reviewDto = new ReviewDto
                                    {
                                        ReviewId = Convert.ToInt32(row["ReviewID"]),
                                        // Average the ratings to get an overall rating
                                        Rating = (Convert.ToDecimal(row["FoodQualityRating"]) + 
                                                 Convert.ToDecimal(row["ServiceRating"]) + 
                                                 Convert.ToDecimal(row["AtmosphereRating"])) / 3m,
                                        Comment = row["Comments"]?.ToString(),
                                        ReviewDate = Convert.ToDateTime(row["CreatedDate"]),
                                        ReviewerUsername = row["ReviewerUsername"]?.ToString()
                                    };
                                    
                                    viewModel.RecentReviews.Add(reviewDto);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error mapping a review row: {Error}", ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue - this is not critical functionality
                        _logger.LogWarning(ex, "Could not get recent reviews. Error: {Message}", ex.Message);
                        // Don't add any reviews - the list will remain empty
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for RestaurantRepHome for User ID {UserId}", userId);
                
                // Add detailed debugging information
                if (ex is SqlException sqlEx)
                {
                    _logger.LogError("SQL Error: Number={Number}, Message={Message}, Procedure={Procedure}", 
                        sqlEx.Number, sqlEx.Message, sqlEx.Procedure);
                }
                
                // Display specific error message for user
                ViewBag.ErrorMessage = "Could not load dashboard data. Error: " + ex.Message;
                
                // For debugging in non-production environment
                ViewBag.DetailedError = ex.ToString();
            }

            return View(viewModel); // Pass the populated ViewModel to the View
        }

        // GET: /RestaurantRepHome/ManageProfile
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ManageProfile()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get the restaurant profile if it exists
                var cmd = new SqlCommand("SELECT * FROM TP_Restaurants WHERE RestaurantID = @UserID");
                cmd.Parameters.AddWithValue("@UserID", userId);
                var ds = _db.GetDataSetUsingCmdObj(cmd);

                // Get available cuisines
                var cuisineCmd = new SqlCommand("SELECT DISTINCT Cuisine FROM TP_Restaurants WHERE Cuisine IS NOT NULL AND Cuisine != '' ORDER BY Cuisine");
                var cuisineDs = _db.GetDataSetUsingCmdObj(cuisineCmd);
                var cuisines = new List<string>();
                
                if (cuisineDs.Tables.Count > 0)
                {
                    foreach (DataRow row in cuisineDs.Tables[0].Rows)
                    {
                        cuisines.Add(row["Cuisine"].ToString());
                    }
                }

                // Add some common cuisines if none exist yet
                if (cuisines.Count == 0)
                {
                    cuisines.AddRange(new[] { "American", "Italian", "Mexican", "Chinese", "Japanese", "Indian", "Thai", "Mediterranean", "French", "Greek" });
                }

                var viewModel = new RestaurantViewModel();
                bool isNewProfile = true;

                if (ds?.Tables[0]?.Rows.Count > 0)
                {
                    var row = ds.Tables[0].Rows[0];
                    viewModel = new RestaurantViewModel
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
                    isNewProfile = false;
                    
                    // Load gallery images
                    if (!isNewProfile)
                    {
                        var galleryCmd = new SqlCommand("TP_spGetRestaurantGalleryImages");
                        galleryCmd.CommandType = CommandType.StoredProcedure;
                        galleryCmd.Parameters.AddWithValue("@RestaurantID", viewModel.RestaurantID);
                        var galleryDs = _db.GetDataSetUsingCmdObj(galleryCmd);
                        
                        if (galleryDs?.Tables.Count > 0 && galleryDs.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow galleryRow in galleryDs.Tables[0].Rows)
                            {
                                viewModel.GalleryImages.Add(new GalleryImageViewModel
                                {
                                    ImageID = Convert.ToInt32(galleryRow["ImageID"]),
                                    RestaurantID = Convert.ToInt32(galleryRow["RestaurantID"]),
                                    ImagePath = galleryRow["ImagePath"].ToString(),
                                    Caption = galleryRow["Caption"]?.ToString() ?? string.Empty,
                                    UploadDate = Convert.ToDateTime(galleryRow["UploadDate"]),
                                    DisplayOrder = Convert.ToInt32(galleryRow["DisplayOrder"])
                                });
                            }
                        }
                    }
                }
                else
                {
                    viewModel.RestaurantID = Convert.ToInt32(userId);
                }

                ViewData["IsNewProfile"] = isNewProfile;
                ViewData["Title"] = isNewProfile ? "Create Restaurant Profile" : "Update Restaurant Profile";
                ViewData["Cuisines"] = new SelectList(cuisines);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading restaurant profile");
                TempData["ErrorMessage"] = "An error occurred while loading your restaurant profile.";
                return RedirectToAction("Index");
            }
        }

        // POST: /RestaurantRepHome/ManageProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "RestaurantRep")]
        public async Task<IActionResult> ManageProfile(RestaurantViewModel model)
        {
            // Retrieve the current user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Validate if the user is authorized to edit this restaurant
            if (model.RestaurantID != 0 && model.RestaurantID.ToString() != userId)
            {
                _logger.LogWarning("User {UserId} attempted to edit restaurant {RestaurantId}", 
                    userId, model.RestaurantID);
                return Forbid();
            }

            // For a new restaurant profile, use the user's ID as the restaurant ID
            if (model.RestaurantID == 0)
            {
                if (int.TryParse(userId, out int userIdInt))
                {
                    model.RestaurantID = userIdInt;
                }
                else
                {
                    // Handle invalid user ID
                    _logger.LogError("Invalid user ID format: {UserId}", userId);
                    TempData["ErrorMessage"] = "Invalid user account. Please contact support.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                // Check if restaurant exists
                SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                checkCmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                
                int restaurantExists = Convert.ToInt32(_db.ExecuteScalarUsingCmdObj(checkCmd));
                
                // Handle file uploads if any
                if (model.ProfilePhotoFile != null && model.ProfilePhotoFile.Length > 0)
                {
                    // Save profile photo using FileStorageService
                    model.ProfilePhoto = await _fileStorageService.SaveRestaurantProfilePhotoAsync(model.ProfilePhotoFile, model.RestaurantID);
                }

                if (model.LogoPhotoFile != null && model.LogoPhotoFile.Length > 0)
                {
                    // Save logo photo using FileStorageService
                    model.LogoPhoto = await _fileStorageService.SaveRestaurantLogoPhotoAsync(model.LogoPhotoFile, model.RestaurantID);
                }
                
                // Handle gallery image files uploads
                if (model.GalleryImageFiles != null && model.GalleryImageFiles.Count > 0)
                {
                    // Process each gallery image using FileStorageService
                    foreach (var imageFile in model.GalleryImageFiles.Take(10)) // Limit to 10 images
                    {
                        if (imageFile.Length > 0 && imageFile.Length <= 2 * 1024 * 1024) // Limit size to 2MB
                        {
                            // Check file type
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                            
                            if (allowedExtensions.Contains(fileExtension))
                            {
                                // Save gallery image
                                string imagePath = await _fileStorageService.SaveRestaurantGalleryImageAsync(imageFile, model.RestaurantID);
                                
                                // Add to database
                                SqlCommand addImageCmd = new SqlCommand("TP_spAddRestaurantGalleryImage");
                                addImageCmd.CommandType = CommandType.StoredProcedure;
                                addImageCmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                                addImageCmd.Parameters.AddWithValue("@ImagePath", imagePath);
                                addImageCmd.Parameters.AddWithValue("@Caption", "");
                                
                                _db.DoUpdateUsingCmdObj(addImageCmd);
                            }
                        }
                    }
                }

                // SQL commands for update/insert
                SqlCommand cmd;
                if (restaurantExists > 0)
                {
                    // Update existing restaurant - use stored procedure
                    cmd = new SqlCommand("dbo.TP_spUpdateRestaurantProfile");
                    cmd.CommandType = CommandType.StoredProcedure;
                }
                else
                {
                    // Insert new restaurant
                    cmd = new SqlCommand(@"
                        INSERT INTO TP_Restaurants (
                            RestaurantID, Name, 
                            Address, City, State, ZipCode, 
                            Cuisine, Hours, Contact, MarketingDescription, 
                            WebsiteURL, SocialMedia, Owner, ProfilePhoto, LogoPhoto, CreatedDate
                        ) VALUES (
                            @RestaurantID, @Name, 
                            @Address, @City, @State, @ZipCode,
                            @Cuisine, @Hours, @Contact, @MarketingDescription,
                            @WebsiteURL, @SocialMedia, @Owner, @ProfilePhoto, @LogoPhoto, @CreatedDate
                        )");
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                }
                
                // Add parameters
                cmd.Parameters.AddWithValue("@RestaurantID", model.RestaurantID);
                cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                cmd.Parameters.AddWithValue("@Address", model.Address ?? "");
                cmd.Parameters.AddWithValue("@City", model.City ?? "");
                cmd.Parameters.AddWithValue("@State", model.State ?? "");
                cmd.Parameters.AddWithValue("@ZipCode", string.IsNullOrEmpty(model.ZipCode) ? (object)DBNull.Value : model.ZipCode);
                cmd.Parameters.AddWithValue("@Cuisine", model.Cuisine ?? "");
                cmd.Parameters.AddWithValue("@Hours", model.Hours ?? "");
                cmd.Parameters.AddWithValue("@Contact", model.Contact ?? "");
                cmd.Parameters.AddWithValue("@MarketingDescription", model.MarketingDescription ?? "");
                cmd.Parameters.AddWithValue("@WebsiteURL", model.WebsiteURL ?? "");
                cmd.Parameters.AddWithValue("@SocialMedia", model.SocialMedia ?? "");
                cmd.Parameters.AddWithValue("@Owner", model.Owner ?? "");
                
                // Handle possible NULL values for photos
                if (string.IsNullOrEmpty(model.ProfilePhoto))
                {
                    cmd.Parameters.AddWithValue("@ProfilePhoto", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@ProfilePhoto", model.ProfilePhoto);
                }
                
                if (string.IsNullOrEmpty(model.LogoPhoto))
                {
                    cmd.Parameters.AddWithValue("@LogoPhoto", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@LogoPhoto", model.LogoPhoto);
                }
                
                // Execute SQL command
                int result = _db.DoUpdateUsingCmdObj(cmd);
                
                // Always assume success unless result is -1 (exception in DoUpdateUsingCmdObj)
                if (result != -1)
                {
                    _logger.LogInformation("Restaurant profile updated successfully for ID {RestaurantId}, result: {Result}", model.RestaurantID, result);
                    TempData["SuccessMessage"] = "Restaurant profile saved successfully!";
                }
                else
                {
                    _logger.LogWarning("Restaurant profile update failed for ID {RestaurantId}, result: {Result}", model.RestaurantID, result);
                    TempData["ErrorMessage"] = "An error occurred while saving the restaurant profile.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving restaurant profile for RestaurantID {RestaurantId}", model.RestaurantID);
                TempData["ErrorMessage"] = "An error occurred while saving your restaurant profile: " + ex.Message;
                return RedirectToAction(nameof(ManageProfile));
            }
        }

        // GET: /RestaurantRepHome/ExportRestaurantData
        [HttpGet]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ExportRestaurantData()
        {
            try
            {
                // Get the user ID (restaurant ID)
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims when trying to export restaurant data");
                    return Forbid();
                }

                int restaurantId = Convert.ToInt32(userId);

                // Get restaurant data from database
                var cmd = new SqlCommand("SELECT * FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                var ds = _db.GetDataSetUsingCmdObj(cmd);

                if (ds?.Tables[0]?.Rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "Restaurant profile not found.";
                    return RedirectToAction("Index");
                }

                var row = ds.Tables[0].Rows[0];
                
                // Map data to restaurant model
                var restaurant = new Shared.Models.Domain.Restaurant
                {
                    RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    Address = row["Address"]?.ToString(),
                    City = row["City"]?.ToString(),
                    State = row["State"]?.ToString(),
                    ZipCode = row["ZipCode"]?.ToString(),
                    Cuisine = row["Cuisine"]?.ToString(),
                    Hours = row["Hours"]?.ToString(),
                    Contact = row["Contact"]?.ToString(),
                    MarketingDescription = row["MarketingDescription"]?.ToString(),
                    WebsiteURL = row["WebsiteURL"]?.ToString(),
                    SocialMedia = row["SocialMedia"]?.ToString(),
                    Owner = row["Owner"]?.ToString(),
                    ProfilePhoto = row["ProfilePhoto"]?.ToString(),
                    LogoPhoto = row["LogoPhoto"]?.ToString(),
                    CreatedDate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.UtcNow
                };

                // Create serializer instance
                var serializer = new Shared.Utilities.SimpleRestaurantSerializer();
                
                // Serialize restaurant data
                string serializedData = serializer.Serialize(restaurant);
                
                // Set filename based on restaurant name and current date
                string safeRestaurantName = restaurant.Name.Replace(" ", "_")
                    .Replace("&", "and")
                    .Replace("'", "")
                    .Replace("\"", "")
                    .Replace("/", "-");
                    
                string fileName = $"{safeRestaurantName}_Export_{DateTime.Now:yyyyMMdd}.xml";
                
                // Return file for download
                var bytes = System.Text.Encoding.UTF8.GetBytes(serializedData);
                return File(bytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting restaurant data");
                TempData["ErrorMessage"] = "An error occurred while exporting restaurant data.";
                return RedirectToAction("Index");
            }
        }

        // GET: /RestaurantRepHome/ImportRestaurantData
        [HttpGet]
        [Authorize(Roles = "RestaurantRep")]
        public IActionResult ImportRestaurantData()
        {
            return View();
        }

        // POST: /RestaurantRepHome/ImportRestaurantData
        [HttpPost]
        [Authorize(Roles = "RestaurantRep")]
        [ValidateAntiForgeryToken]
        public IActionResult ImportRestaurantData(IFormFile importFile)
        {
            try
            {
                // Validate file
                if (importFile == null || importFile.Length == 0)
                {
                    ModelState.AddModelError("", "Please select a file to import.");
                    return View();
                }
                
                // Get the user ID (restaurant ID)
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims when trying to import restaurant data");
                    return Forbid();
                }
                
                int restaurantId = Convert.ToInt32(userId);
                
                // Read file content
                string xmlContent;
                using (var reader = new StreamReader(importFile.OpenReadStream()))
                {
                    xmlContent = reader.ReadToEnd();
                }
                
                // Create serializer instance
                var serializer = new Shared.Utilities.SimpleRestaurantSerializer();
                
                // Deserialize data
                Shared.Models.Domain.Restaurant importedRestaurant;
                try
                {
                    importedRestaurant = serializer.Deserialize(xmlContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid restaurant data file format");
                    ModelState.AddModelError("", "The file is not a valid restaurant data file.");
                    return View();
                }
                
                // Override imported ID with current user ID for security
                importedRestaurant.RestaurantID = restaurantId;
                
                // Check if restaurant profile exists
                var checkCmd = new SqlCommand("SELECT 1 FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                checkCmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                var exists = _db.ExecuteScalarUsingCmdObj(checkCmd) != null;
                
                SqlCommand cmd;
                if (exists)
                {
                    // Update existing record
                    cmd = new SqlCommand(@"
                        UPDATE TP_Restaurants SET 
                            Name = @Name, 
                            Address = @Address, 
                            City = @City, 
                            State = @State, 
                            ZipCode = @ZipCode, 
                            Cuisine = @Cuisine, 
                            Hours = @Hours, 
                            Contact = @Contact, 
                            MarketingDescription = @MarketingDescription, 
                            WebsiteURL = @WebsiteURL, 
                            SocialMedia = @SocialMedia, 
                            Owner = @Owner,
                            ProfilePhoto = @ProfilePhoto,
                            LogoPhoto = @LogoPhoto
                        WHERE RestaurantID = @RestaurantID");
                }
                else
                {
                    // Insert new record
                    cmd = new SqlCommand(@"
                        INSERT INTO TP_Restaurants (
                            RestaurantID, Name, Address, City, State, ZipCode, 
                            Cuisine, Hours, Contact, MarketingDescription, 
                            WebsiteURL, SocialMedia, Owner, ProfilePhoto, LogoPhoto, CreatedDate
                        ) VALUES (
                            @RestaurantID, @Name, @Address, @City, @State, @ZipCode, 
                            @Cuisine, @Hours, @Contact, @MarketingDescription, 
                            @WebsiteURL, @SocialMedia, @Owner, @ProfilePhoto, @LogoPhoto, @CreatedDate
                        )");
                    
                    // Add CreatedDate parameter
                    cmd.Parameters.AddWithValue("@CreatedDate", importedRestaurant.CreatedDate);
                }
                
                // Add common parameters
                cmd.Parameters.AddWithValue("@RestaurantID", importedRestaurant.RestaurantID);
                cmd.Parameters.AddWithValue("@Name", importedRestaurant.Name);
                cmd.Parameters.AddWithValue("@Address", (object)importedRestaurant.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@City", (object)importedRestaurant.City ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@State", (object)importedRestaurant.State ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ZipCode", (object)importedRestaurant.ZipCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cuisine", (object)importedRestaurant.Cuisine ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hours", (object)importedRestaurant.Hours ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Contact", (object)importedRestaurant.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MarketingDescription", (object)importedRestaurant.MarketingDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WebsiteURL", (object)importedRestaurant.WebsiteURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SocialMedia", (object)importedRestaurant.SocialMedia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Owner", (object)importedRestaurant.Owner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object)importedRestaurant.ProfilePhoto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LogoPhoto", (object)importedRestaurant.LogoPhoto ?? DBNull.Value);
                
                // Execute query
                int result = _db.DoUpdateUsingCmdObj(cmd);
                
                if (result > 0)
                {
                    TempData["SuccessMessage"] = exists 
                        ? "Restaurant profile imported and updated successfully!" 
                        : "Restaurant profile imported and created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Failed to import restaurant data. Please try again.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing restaurant data");
                ModelState.AddModelError("", "An error occurred while importing restaurant data.");
                return View();
            }
        }

        // --- Action for Manage Reservations button ---
        public IActionResult ManageReservations()
        {
            _logger.LogInformation("Loading Manage Reservations page.");
            
            try
            {
                // Get the user ID (restaurant ID)
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("ManageReservations: User ID not found in claims");
                    return RedirectToAction("Login", "Account");
                }
                
                _logger.LogInformation("ManageReservations: Retrieved user ID: {UserId}", userId);
                
                // Create command to call the stored procedure
                var cmd = new SqlCommand("TP_spGetReservationsForRestaurant");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", userId);
                
                _logger.LogInformation("ManageReservations: Executing stored procedure TP_spGetReservationsForRestaurant");
                
                // Add try-catch specifically for database operation
                DataSet ds;
                try
                {
                    ds = _db.GetDataSetUsingCmdObj(cmd);
                    _logger.LogInformation("ManageReservations: DataSet retrieved successfully");
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Error in ManageReservations: Number={Number}, Message={Message}, Procedure={Procedure}", 
                        sqlEx.Number, sqlEx.Message, sqlEx.Procedure);
                    
                    // Fallback to direct SQL if the stored procedure fails
                    _logger.LogWarning("ManageReservations: Falling back to direct SQL query");
                    
                    var fallbackCmd = new SqlCommand(@"
                        SELECT 
                            ReservationID, 
                            RestaurantID, 
                            UserID, 
                            ReservationDateTime, 
                            PartySize, 
                            ContactName, 
                            ContactName AS CustomerName, 
                            ContactPhone AS Phone, 
                            ContactEmail AS Email, 
                            SpecialRequests, 
                            Status
                        FROM 
                            TP_Reservations 
                        WHERE 
                            RestaurantID = @RestaurantID 
                        ORDER BY 
                            ReservationDateTime DESC");
                    fallbackCmd.Parameters.AddWithValue("@RestaurantID", userId);
                    
                    try
                    {
                        ds = _db.GetDataSetUsingCmdObj(fallbackCmd);
                        _logger.LogInformation("ManageReservations: Fallback SQL query executed successfully");
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Error executing fallback SQL query in ManageReservations");
                        ViewBag.ErrorMessage = "Could not load reservations due to database error. Please try again later.";
                        ViewBag.DetailedError = $"SQL Error: {sqlEx.Message}. Fallback Error: {fallbackEx.Message}";
                        return View(new List<Reservation>());
                    }
                }
                
                var reservations = new List<Reservation>();
                
                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    _logger.LogInformation("ManageReservations: Found {Count} reservations", ds.Tables[0].Rows.Count);
                    
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        try
                        {
                            // Log column names for debugging
                            if (reservations.Count == 0)
                            {
                                var columnNames = string.Join(", ", row.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                                _logger.LogInformation("ManageReservations: Available columns: {Columns}", columnNames);
                            }
                            
                            // Using a local variable to create the reservation to make debugging and code clarity better
                            var reservation = new Reservation
                            {
                                ReservationID = Convert.ToInt32(row["ReservationID"]),
                                RestaurantID = Convert.ToInt32(row["RestaurantID"]),
                                UserID = row["UserID"] != DBNull.Value ? Convert.ToInt32(row["UserID"]) : null,
                                ReservationDateTime = Convert.ToDateTime(row["ReservationDateTime"]),
                                PartySize = Convert.ToInt32(row["PartySize"]),
                                ContactName = row["ContactName"]?.ToString() ?? row["CustomerName"]?.ToString() ?? "Guest",
                                SpecialRequests = row["SpecialRequests"]?.ToString(),
                                Status = row["Status"]?.ToString() ?? "Pending"
                            };
                            
                            // The database fields are "Phone" and "Email" but the properties are "ContactPhone" and "ContactEmail"
                            // Using the special property setters to ensure correct mapping
                            if (row["Phone"] != DBNull.Value)
                                reservation.ContactPhone = row["Phone"].ToString();
                                
                            if (row["Email"] != DBNull.Value)
                                reservation.ContactEmail = row["Email"].ToString();
                                
                            reservations.Add(reservation);
                        }
                        catch (Exception rowEx)
                        {
                            _logger.LogError(rowEx, "Error mapping DataRow to Reservation object");
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("ManageReservations: No reservations found for restaurant {RestaurantId}", userId);
                }
                
                ViewBag.SuccessMessage = TempData["SuccessMessage"]?.ToString();
                ViewBag.ErrorMessage = TempData["ErrorMessage"]?.ToString();
                
                return View(reservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reservations in ManageReservations. Exception type: {ExceptionType}", ex.GetType().Name);
                TempData["ErrorMessage"] = "An error occurred while loading reservations.";
                return RedirectToAction("Index");
            }
        }

        // --- Action to confirm a reservation ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmReservation(int id)
        {
            _logger.LogInformation("ConfirmReservation: Starting confirmation for reservation ID {ReservationId}", id);
            
            try
            {
                var cmd = new SqlCommand("TP_spUpdateReservationStatus");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);
                cmd.Parameters.AddWithValue("@NewStatus", "Confirmed");
                
                _logger.LogInformation("ConfirmReservation: Executing stored procedure TP_spUpdateReservationStatus");
                
                try
                {
                    int result = _db.DoUpdateUsingCmdObj(cmd);
                    _logger.LogInformation("ConfirmReservation: Update result: {Result}", result);
                    
                    TempData["SuccessMessage"] = "Reservation confirmed successfully.";
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Error in ConfirmReservation: Number={Number}, Message={Message}, Procedure={Procedure}", 
                        sqlEx.Number, sqlEx.Message, sqlEx.Procedure);
                    
                    // Fallback to direct SQL if the stored procedure fails
                    _logger.LogWarning("ConfirmReservation: Falling back to direct SQL query");
                    
                    var fallbackCmd = new SqlCommand("UPDATE TP_Reservations SET Status = 'Confirmed' WHERE ReservationID = @ReservationID");
                    fallbackCmd.Parameters.AddWithValue("@ReservationID", id);
                    
                    try
                    {
                        int result = _db.DoUpdateUsingCmdObj(fallbackCmd);
                        _logger.LogInformation("ConfirmReservation: Fallback update result: {Result}", result);
                        
                        TempData["SuccessMessage"] = "Reservation confirmed successfully.";
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Error executing fallback SQL query in ConfirmReservation");
                        TempData["ErrorMessage"] = "Failed to confirm reservation. Please try again later.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming reservation {ReservationID}. Exception type: {ExceptionType}", id, ex.GetType().Name);
                TempData["ErrorMessage"] = "An error occurred while confirming the reservation.";
            }
            
            return RedirectToAction("ManageReservations");
        }

        // --- Action to cancel a reservation ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelReservation(int id)
        {
            _logger.LogInformation("CancelReservation: Starting cancellation for reservation ID {ReservationId}", id);
            
            try
            {
                var cmd = new SqlCommand("TP_spUpdateReservationStatus");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);
                cmd.Parameters.AddWithValue("@NewStatus", "Cancelled");
                
                _logger.LogInformation("CancelReservation: Executing stored procedure TP_spUpdateReservationStatus");
                
                try
                {
                    int result = _db.DoUpdateUsingCmdObj(cmd);
                    _logger.LogInformation("CancelReservation: Update result: {Result}", result);
                    
                    TempData["SuccessMessage"] = "Reservation cancelled successfully.";
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Error in CancelReservation: Number={Number}, Message={Message}, Procedure={Procedure}", 
                        sqlEx.Number, sqlEx.Message, sqlEx.Procedure);
                    
                    // Fallback to direct SQL if the stored procedure fails
                    _logger.LogWarning("CancelReservation: Falling back to direct SQL query");
                    
                    var fallbackCmd = new SqlCommand("UPDATE TP_Reservations SET Status = 'Cancelled' WHERE ReservationID = @ReservationID");
                    fallbackCmd.Parameters.AddWithValue("@ReservationID", id);
                    
                    try
                    {
                        int result = _db.DoUpdateUsingCmdObj(fallbackCmd);
                        _logger.LogInformation("CancelReservation: Fallback update result: {Result}", result);
                        
                        TempData["SuccessMessage"] = "Reservation cancelled successfully.";
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Error executing fallback SQL query in CancelReservation");
                        TempData["ErrorMessage"] = "Failed to cancel reservation. Please try again later.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation {ReservationID}. Exception type: {ExceptionType}", id, ex.GetType().Name);
                TempData["ErrorMessage"] = "An error occurred while cancelling the reservation.";
            }
            
            return RedirectToAction("ManageReservations");
        }

        // --- Action to delete a reservation ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteReservation(int id)
        {
            _logger.LogInformation("DeleteReservation: Starting deletion for reservation ID {ReservationId}", id);
            
            try
            {
                var cmd = new SqlCommand("TP_spDeleteReservation");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);
                
                _logger.LogInformation("DeleteReservation: Executing stored procedure TP_spDeleteReservation");
                
                try
                {
                    int result = _db.DoUpdateUsingCmdObj(cmd);
                    _logger.LogInformation("DeleteReservation: Delete result: {Result}", result);
                    
                    if (result > 0)
                    {
                        TempData["SuccessMessage"] = "Reservation deleted successfully.";
                    }
                    else
                    {
                        _logger.LogWarning("DeleteReservation: No rows affected by delete operation");
                        TempData["ErrorMessage"] = "Failed to delete reservation. Reservation may not exist.";
                    }
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, "SQL Error in DeleteReservation: Number={Number}, Message={Message}, Procedure={Procedure}", 
                        sqlEx.Number, sqlEx.Message, sqlEx.Procedure);
                    
                    // Fallback to direct SQL if the stored procedure fails
                    _logger.LogWarning("DeleteReservation: Falling back to direct SQL query");
                    
                    var fallbackCmd = new SqlCommand("DELETE FROM TP_Reservations WHERE ReservationID = @ReservationID");
                    fallbackCmd.Parameters.AddWithValue("@ReservationID", id);
                    
                    try
                    {
                        int result = _db.DoUpdateUsingCmdObj(fallbackCmd);
                        _logger.LogInformation("DeleteReservation: Fallback delete result: {Result}", result);
                        
                        if (result > 0)
                        {
                            TempData["SuccessMessage"] = "Reservation deleted successfully.";
                        }
                        else
                        {
                            _logger.LogWarning("DeleteReservation: No rows affected by fallback delete operation");
                            TempData["ErrorMessage"] = "Failed to delete reservation. Reservation may not exist.";
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Error executing fallback SQL query in DeleteReservation");
                        TempData["ErrorMessage"] = "Failed to delete reservation. Please try again later.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reservation {ReservationID}. Exception type: {ExceptionType}", id, ex.GetType().Name);
                TempData["ErrorMessage"] = "An error occurred while deleting the reservation.";
            }
            
            return RedirectToAction("ManageReservations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadGalleryImage(int restaurantId, IFormFile imageFile, string caption = "")
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "No file selected.";
                return RedirectToAction("ManageGallery", new { id = restaurantId });
            }

            // Validate file size (5MB limit)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Image file is too large. Maximum size is 5MB.";
                return RedirectToAction("ManageGallery", new { id = restaurantId });
            }

            // Validate file type
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || 
                !new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extension))
            {
                TempData["ErrorMessage"] = "Invalid image file type. Supported formats: JPG, PNG, GIF.";
                return RedirectToAction("ManageGallery", new { id = restaurantId });
            }

            try
            {
                // Option 1: Continue using the API approach
                // Create form data to send to API
                var formData = new MultipartFormDataContent();
                
                // Add the file
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    var fileContent = new ByteArrayContent(memoryStream.ToArray());
                    formData.Add(fileContent, "image", imageFile.FileName);
                }

                // Send to API
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = $"api/RestaurantsApi/{restaurantId}/Images";
                
                var response = await client.PostAsync(apiUrl, formData);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ImageUploadResult>();
                    
                    // If a caption was provided, update it in the database
                    if (!string.IsNullOrEmpty(caption) && result?.imageId > 0)
                    {
                        // Update caption - this would be a separate API endpoint
                        // For now, we'll just accept that the caption is empty initially
                    }
                    
                    TempData["SuccessMessage"] = "Image uploaded successfully!";
                    return RedirectToAction("ManageGallery", new { id = restaurantId });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed: Could not upload image. Status: {StatusCode}, Reason: {ApiError}", 
                        response.StatusCode, errorContent);
                    
                    // Option 2: Fallback to FileStorageService if API fails
                    var imagePath = await _fileStorageService.SaveRestaurantGalleryImageAsync(imageFile, restaurantId);
                    
                    // Add to database
                    SqlCommand cmd = new SqlCommand("TP_spAddRestaurantGalleryImage");
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                    cmd.Parameters.AddWithValue("@ImagePath", imagePath);
                    cmd.Parameters.AddWithValue("@Caption", caption ?? "");
                    _db.DoUpdateUsingCmdObj(cmd);
                    
                    TempData["SuccessMessage"] = "Image uploaded successfully (using fallback method)!";
                    return RedirectToAction("ManageGallery", new { id = restaurantId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for Restaurant {RestaurantId}", restaurantId);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction("ManageGallery", new { id = restaurantId });
            }
        }

        // Helper class for API response
        private class ImageUploadResult
        {
            public int imageId { get; set; }
            public string filePath { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGalleryImage(int imageId, int restaurantId)
        {
            try
            {
                // Send to API
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = $"api/RestaurantsApi/GalleryImages/{imageId}";
                
                var response = await client.DeleteAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Image deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed: Could not delete image. Status: {StatusCode}, Reason: {ApiError}", 
                        response.StatusCode, errorContent);
                    
                    TempData["ErrorMessage"] = "Error deleting image. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image ID {ImageId} for Restaurant {RestaurantId}", imageId, restaurantId);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
            }
            
            return RedirectToAction("ManageGallery", new { id = restaurantId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateImageCaption(int imageId, int restaurantId, string caption)
        {
            try
            {
                // Send to API
                var client = _httpClientFactory.CreateClient("Project3Api");
                string apiUrl = $"api/RestaurantsApi/GalleryImages/{imageId}/Caption";
                
                var content = new StringContent(JsonSerializer.Serialize(new { caption }), 
                    System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.PutAsync(apiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Caption updated successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API call failed: Could not update caption. Status: {StatusCode}, Reason: {ApiError}", 
                        response.StatusCode, errorContent);
                    
                    TempData["ErrorMessage"] = "Error updating caption. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating caption for image ID {ImageId}, Restaurant {RestaurantId}", imageId, restaurantId);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
            }
            
            return RedirectToAction("ManageGallery", new { id = restaurantId });
        }

        [HttpGet]
        public async Task<IActionResult> ManageGallery(int id)
        {
            // Use direct SQL to get the restaurant details including gallery images
            try
            {
                // Check if current user is authorized to access this restaurant
                var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(currentUserIdString, out int currentUserId) || currentUserId != id)
                {
                    return Forbid();
                }
                
                // Get restaurant details directly from database
                SqlCommand cmd = new SqlCommand("SELECT * FROM TP_Restaurants WHERE RestaurantID = @RestaurantID");
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                
                DataSet ds = _db.GetDataSetUsingCmdObj(cmd);
                
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    _logger.LogWarning("Restaurant not found: {RestaurantId}", id);
                    return NotFound();
                }
                
                // Map to view model
                var restaurant = new RestaurantViewModel
                {
                    RestaurantID = id,
                    Name = ds.Tables[0].Rows[0]["Name"]?.ToString(),
                    // Add other properties as needed
                };
                
                // Get gallery images
                SqlCommand imagesCmd = new SqlCommand("SELECT * FROM TP_RestaurantImages WHERE RestaurantID = @RestaurantID ORDER BY DisplayOrder, UploadDate DESC");
                imagesCmd.Parameters.AddWithValue("@RestaurantID", id);
                
                DataSet imagesDs = _db.GetDataSetUsingCmdObj(imagesCmd);
                
                var galleryImages = new List<GalleryImageViewModel>();
                
                if (imagesDs != null && imagesDs.Tables.Count > 0 && imagesDs.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in imagesDs.Tables[0].Rows)
                    {
                        galleryImages.Add(new GalleryImageViewModel
                        {
                            ImageID = Convert.ToInt32(row["ImageID"]),
                            RestaurantID = id,
                            ImagePath = row["ImagePath"]?.ToString(),
                            Caption = row["Caption"]?.ToString(),
                            UploadDate = row["UploadDate"] != DBNull.Value ? Convert.ToDateTime(row["UploadDate"]) : DateTime.MinValue,
                            DisplayOrder = row["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(row["DisplayOrder"]) : 0
                        });
                    }
                }
                
                restaurant.GalleryImages = galleryImages;
                
                return View(restaurant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManageGallery action for restaurant {RestaurantId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading gallery management.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Other actions for RestaurantRepHomeController if needed...
    }
}
