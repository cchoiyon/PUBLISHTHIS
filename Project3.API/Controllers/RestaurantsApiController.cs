using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project3.Shared.Models.Domain;
using Project3.Shared.Models.DTOs;
using Project3.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;

namespace Project3.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantsApiController : ControllerBase
    {
        private readonly ILogger<RestaurantsApiController> _logger;
        private readonly Connection _dbConnect;

        public RestaurantsApiController(ILogger<RestaurantsApiController> logger, Connection dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        [HttpGet("{id:int}", Name = "GetRestaurantById")]
        [AllowAnonymous]
        public async Task<ActionResult<Restaurant>> GetRestaurantById(int id)
        {
            _logger.LogInformation("API: Attempting to get restaurant by ID: {RestaurantId}", id);
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetRestaurantByID");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", id);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    Restaurant restaurant = MapDataRowToRestaurant(ds.Tables[0].Rows[0]);
                    if (restaurant != null)
                    {
                        _logger.LogInformation("API: Found restaurant ID: {RestaurantId}", id);
                        return Ok(restaurant);
                    }
                    else { return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error processeing restaurant data.")); }
                }
                else
                {
                    _logger.LogWarning("API: Restaurant not found for ID: {RestaurantId}", id);
                    return NotFound(new ErrorResponseDto($"Restaurant with ID {id} not found."));
                }
            }
            catch (SqlException sqlEx) { return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Database error retreiving restaurant data.")); }
            catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error retreiving restaurant data.")); }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> UpdateRestaurantProfile(int id, [FromBody] UpdateRestaurantProfileDto profileDto)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized(new ErrorResponseDto("User identifier claim is invalid or missing.")); }

            _logger.LogInformation("API: Attempting to update profile for Restaurant {RestaurantId} by Rep {RepUserId}", id, authenticatedUserId);

            bool isAuthorized = User.IsInRole("restaurantRep") && authenticatedUserId == id;
            if (!isAuthorized)
            {
                _logger.LogWarning("API: Rep {RepUserId} forbidden to update profile for Restaurant {RestaurantId}.", authenticatedUserId, id);
                return Forbid();
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != profileDto.RestaurantID) return BadRequest(new ErrorResponseDto("Restaurant ID mismatch between route and body."));


            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spUpdateRestaurantProfile");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RestaurantID", id);
                cmd.Parameters.AddWithValue("@Name", profileDto.Name);
                cmd.Parameters.AddWithValue("@Address", (object)profileDto.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@City", (object)profileDto.City ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@State", (object)profileDto.State ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ZipCode", (object)profileDto.ZipCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cuisine", (object)profileDto.Cuisine ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Hours", (object)profileDto.Hours ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Contact", (object)profileDto.Contact ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MarketingDescription", (object)profileDto.MarketingDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WebsiteURL", (object)profileDto.WebsiteURL ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SocialMedia", (object)profileDto.SocialMedia ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Owner", (object)profileDto.Owner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProfilePhoto", (object)profileDto.ProfilePhoto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LogoPhoto", (object)profileDto.LogoPhoto ?? DBNull.Value);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Profile updated successfully for Restaurant {RestaurantId} by Rep {RepUserId}", id, authenticatedUserId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Profile update failed for Restaurant {RestaurantId} (DB update returned 0 rows affected).", id);
                    return NotFound(new ErrorResponseDto($"Restaurant with ID {id} not found or update failled."));
                }
            }
            catch (SqlException sqlEx) { return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Database error updateing restaurant profile.")); }
            catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error updateing restaurant profile.")); }
        }

        [HttpPost("{id:int}/Images")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> UploadRestaurantImage(int id, [FromForm] IFormFile image)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized(new ErrorResponseDto("User identifier claim is invalid or missing.")); }
            
            _logger.LogInformation("API: Attempting to upload image for Restaurant {RestaurantId} by Rep {RepUserId}", id, authenticatedUserId);
            
            bool isAuthorized = User.IsInRole("restaurantRep") && authenticatedUserId == id;
            if (!isAuthorized)
            {
                _logger.LogWarning("API: Rep {RepUserId} forbidden to upload image for Restaurant {RestaurantId}", authenticatedUserId, id);
                return Forbid();
            }
            
            if (image == null || image.Length == 0)
            {
                return BadRequest(new ErrorResponseDto("No image file provided."));
            }
            
            if (image.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new ErrorResponseDto("Image file is too large. Maximum size is 5MB."));
            }
            
            string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || 
                !new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extension))
            {
                return BadRequest(new ErrorResponseDto("Invalid image file type. Supported formats: JPG, PNG, GIF."));
            }
            
            try
            {
                string uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }
                
                string uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(uploadsDirectory, uniqueFileName);
                string dbFilePath = $"FileStorage/{uniqueFileName}";
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                
                SqlCommand cmd = new SqlCommand("dbo.TP_spAddRestaurantGalleryImage");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", id);
                cmd.Parameters.AddWithValue("@ImagePath", dbFilePath);
                cmd.Parameters.AddWithValue("@Caption", string.Empty);
                
                object imageIdObj = _dbConnect.ExecuteScalarUsingCmdObj(cmd);
                if (imageIdObj != null && imageIdObj != DBNull.Value)
                {
                    int imageId = Convert.ToInt32(imageIdObj);
                    _logger.LogInformation("API: Image uploaded successfully for Restaurant {RestaurantId}, Image ID: {ImageId}", id, imageId);
                    return CreatedAtAction(nameof(GetRestaurantById), new { id = id }, new { imageId = imageId, filePath = dbFilePath });
                }
                else
                {
                    _logger.LogError("API: Failed to save image path to database for Restaurant {RestaurantId}", id);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Failed to save image data."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error uploading image for Restaurant {RestaurantId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error uploading image. Please try again later."));
            }
        }

        [HttpDelete("GalleryImages/{imageId:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> DeleteGalleryImage(int imageId)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized(new ErrorResponseDto("User identifier claim is invalid or missing.")); }
            
            _logger.LogInformation("API: Attempting to delete gallery image with ID: {ImageId} by Rep {RepUserId}", imageId, authenticatedUserId);
            
            try
            {
                SqlCommand getImageCmd = new SqlCommand("SELECT RestaurantID, ImagePath FROM TP_RestaurantGalleryImages WHERE ImageID = @ImageID");
                getImageCmd.Parameters.AddWithValue("@ImageID", imageId);
                
                DataSet imageDs = _dbConnect.GetDataSetUsingCmdObj(getImageCmd);
                
                if (imageDs == null || imageDs.Tables.Count == 0 || imageDs.Tables[0].Rows.Count == 0)
                {
                    _logger.LogWarning("API: Gallery image with ID {ImageId} not found", imageId);
                    return NotFound(new ErrorResponseDto($"Gallery image with ID {imageId} not found."));
                }
                
                DataRow imageRow = imageDs.Tables[0].Rows[0];
                int restaurantId = Convert.ToInt32(imageRow["RestaurantID"]);
                string imagePath = imageRow["ImagePath"]?.ToString();
                
                bool isAuthorized = User.IsInRole("restaurantRep") && authenticatedUserId == restaurantId;
                if (!isAuthorized)
                {
                    _logger.LogWarning("API: Rep {RepUserId} forbidden to delete image {ImageId} for Restaurant {RestaurantId}", authenticatedUserId, imageId, restaurantId);
                    return Forbid();
                }
                
                SqlCommand deleteCmd = new SqlCommand("dbo.TP_spDeleteRestaurantGalleryImage");
                deleteCmd.CommandType = CommandType.StoredProcedure;
                deleteCmd.Parameters.AddWithValue("@ImageID", imageId);
                
                int result = _dbConnect.DoUpdateUsingCmdObj(deleteCmd);
                
                if (result > 0)
                {
                    _logger.LogInformation("API: Gallery image {ImageId} deleted successfully from Restaurant {RestaurantId} by Rep {RepUserId}", 
                        imageId, restaurantId, authenticatedUserId);
                        
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), imagePath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            try
                            {
                                System.IO.File.Delete(fullPath);
                                _logger.LogInformation("API: Physical file deleted for Gallery image {ImageId}", imageId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "API: Could not delete physical file for Gallery image {ImageId}", imageId);
                            }
                        }
                    }
                    
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Gallery image {ImageId} delete failed (DB update returned 0 rows affected).", imageId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Failed to delete gallery image."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error deleting gallery image {ImageId}", imageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error deleteing gallery image."));
            }
        }

        [HttpPut("GalleryImages/{imageId:int}/Caption")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> UpdateImageCaption(int imageId, [FromBody] UpdateCaptionDto captionDto)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized(new ErrorResponseDto("User identifier claim is invalid or missing.")); }
            
            _logger.LogInformation("API: Attempting to update caption for gallery image {ImageId} by Rep {RepUserId}", 
                imageId, authenticatedUserId);
            
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            try
            {
                SqlCommand getImageCmd = new SqlCommand("SELECT RestaurantID FROM TP_RestaurantGalleryImages WHERE ImageID = @ImageID");
                getImageCmd.Parameters.AddWithValue("@ImageID", imageId);
                
                object restaurantIdObj = _dbConnect.ExecuteScalarUsingCmdObj(getImageCmd);
                
                if (restaurantIdObj == null || restaurantIdObj == DBNull.Value)
                {
                    _logger.LogWarning("API: Gallery image {ImageId} not found", imageId);
                    return NotFound(new ErrorResponseDto($"Gallery image with ID {imageId} not found."));
                }
                
                int restaurantId = Convert.ToInt32(restaurantIdObj);
                
                bool isAuthorized = User.IsInRole("restaurantRep") && authenticatedUserId == restaurantId;
                if (!isAuthorized)
                {
                    _logger.LogWarning("API: Rep {RepUserId} forbidden to update caption for image {ImageId} for Restaurant {RestaurantId}", 
                        authenticatedUserId, imageId, restaurantId);
                    return Forbid();
                }
                
                SqlCommand updateCmd = new SqlCommand("UPDATE TP_RestaurantGalleryImages SET Caption = @Caption WHERE ImageID = @ImageID");
                updateCmd.Parameters.AddWithValue("@ImageID", imageId);
                updateCmd.Parameters.AddWithValue("@Caption", captionDto.Caption ?? string.Empty);
                
                int result = _dbConnect.DoUpdateUsingCmdObj(updateCmd);
                
                if (result > 0)
                {
                    _logger.LogInformation("API: Caption updated successfully for Gallery image {ImageId} by Rep {RepUserId}", 
                        imageId, authenticatedUserId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Caption update failed for Gallery image {ImageId} (DB update returned 0 rows affected).", imageId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Failed to update caption."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error updateing caption for Gallery image {ImageId}", imageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto("Error updateing caption."));
            }
        }

        public class UpdateCaptionDto
        {
            public string Caption { get; set; }
        }

        private Restaurant MapDataRowToRestaurant(DataRow dr)
        {
            if (dr == null) return null;
            
            try
            {
                var restaurant = new Restaurant
                {
                    RestaurantID = dr.Table.Columns.Contains("RestaurantID") ? Convert.ToInt32(dr["RestaurantID"]) : 0,
                    Name = dr.Table.Columns.Contains("Name") ? dr["Name"]?.ToString() : string.Empty,
                    Address = dr.Table.Columns.Contains("Address") ? dr["Address"]?.ToString() : null,
                    City = dr.Table.Columns.Contains("City") ? dr["City"]?.ToString() : null,
                    State = dr.Table.Columns.Contains("State") ? dr["State"]?.ToString() : null,
                    ZipCode = dr.Table.Columns.Contains("ZipCode") ? dr["ZipCode"]?.ToString() : null,
                    Cuisine = dr.Table.Columns.Contains("Cuisine") ? dr["Cuisine"]?.ToString() : null,
                    Hours = dr.Table.Columns.Contains("Hours") ? dr["Hours"]?.ToString() : null,
                    Contact = dr.Table.Columns.Contains("Contact") ? dr["Contact"]?.ToString() : null,
                    MarketingDescription = dr.Table.Columns.Contains("MarketingDescription") ? dr["MarketingDescription"]?.ToString() : null,
                    WebsiteURL = dr.Table.Columns.Contains("WebsiteURL") ? dr["WebsiteURL"]?.ToString() : null,
                    SocialMedia = dr.Table.Columns.Contains("SocialMedia") ? dr["SocialMedia"]?.ToString() : null,
                    Owner = dr.Table.Columns.Contains("Owner") ? dr["Owner"]?.ToString() : null,
                    ProfilePhoto = dr.Table.Columns.Contains("ProfilePhoto") ? dr["ProfilePhoto"]?.ToString() : null,
                    LogoPhoto = dr.Table.Columns.Contains("LogoPhoto") ? dr["LogoPhoto"]?.ToString() : null
                };
                
                return restaurant;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private List<Restaurant> MapDataSetToRestaurantList(DataSet ds)
        {
            var restaurants = new List<Restaurant>();
            
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Restaurant restaurant = MapDataRowToRestaurant(dr);
                    if (restaurant != null)
                    {
                        restaurants.Add(restaurant);
                    }
                }
            }
            
            return restaurants;
        }
    }
} 