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

namespace Project3.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsApiController : ControllerBase
    {
        private readonly ILogger<ReviewsApiController> _logger;
        private readonly Connection _dbConnect;

        public ReviewsApiController(
            ILogger<ReviewsApiController> logger, 
            Connection dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        [HttpGet("restaurant/{restaurantId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForRestaurant(int restaurantId)
        {
            _logger.LogInformation("API: Getting reviews for restaurant ID {RestaurantId}", restaurantId);

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReviewsByRestaurant");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                List<Review> reviews = MapDataSetToReviewList(ds);

                return Ok(reviews);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error getting reviews for restaurant ID {RestaurantId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    restaurantId, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error retreiving reviews.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error getting reviews for restaurant ID {RestaurantId}", restaurantId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retreiving reviews.");
            }
        }

        [HttpGet("user/{userId:int}")]
        [Authorize(Roles = "admin,reviewer")]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsByUser(int userId)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            bool isAuthorized = authenticatedUserId == userId || User.IsInRole("admin");
            if (!isAuthorized)
            {
                _logger.LogWarning("API: Unauthorized attempt to access reviews for user ID {UserId} by user ID {AuthenticatedUserId}", userId, authenticatedUserId);
                return Forbid();
            }

            _logger.LogInformation("API: Getting reviews by user ID {UserId}", userId);

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReviewsByUser");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserID", userId);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                List<Review> reviews = MapDataSetToReviewList(ds);

                return Ok(reviews);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error getting reviews for user ID {UserId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    userId, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error retreiving reviews.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error getting reviews for user ID {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retreiving reviews.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "reviewer")]
        public async Task<ActionResult<Review>> CreateReview([FromBody] CreateReviewDto reviewDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            _logger.LogInformation("API: Attempting to create review for Restaurant {RestaurantId} by User {UserId}", 
                reviewDto.RestaurantID, authenticatedUserId);

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spAddReview");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", reviewDto.RestaurantID);
                cmd.Parameters.AddWithValue("@UserID", authenticatedUserId);
                cmd.Parameters.AddWithValue("@VisitDate", reviewDto.VisitDate);
                cmd.Parameters.AddWithValue("@Comments", reviewDto.Comments);
                cmd.Parameters.AddWithValue("@FoodQualityRating", reviewDto.FoodQualityRating);
                cmd.Parameters.AddWithValue("@ServiceRating", reviewDto.ServiceRating);
                cmd.Parameters.AddWithValue("@AtmosphereRating", reviewDto.AtmosphereRating);
                cmd.Parameters.AddWithValue("@PriceRating", reviewDto.PriceRating);

                object result = _dbConnect.ExecuteScalarFunction(cmd);
                int newReviewId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                if (newReviewId > 0)
                {
                    _logger.LogInformation("API: Review {ReviewId} created for Restaurant {RestaurantId} by User {UserId}", 
                        newReviewId, reviewDto.RestaurantID, authenticatedUserId);

                    Review createdReview = GetReviewByIdInternal(newReviewId);
                    
                    if (createdReview != null)
                    {
                        return CreatedAtAction(nameof(GetReviewById), new { id = newReviewId }, createdReview);
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, "Error retreiving created review.");
                    }
                }
                else
                {
                    _logger.LogError("API: Failed to add review record to DB (ExecuteScalar returned 0 or null) for Restaurant {RestaurantId}", 
                        reviewDto.RestaurantID);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error saveing review data.");
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error creating review for Restaurant {RestaurantId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    reviewDto.RestaurantID, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error createing review.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error creating review for Restaurant {RestaurantId}", reviewDto.RestaurantID);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error createing review.");
            }
        }

        [HttpGet("{id:int}", Name = "GetReviewById")]
        [AllowAnonymous]
        public async Task<ActionResult<Review>> GetReviewById(int id)
        {
            _logger.LogInformation("API: Getting review by ID {ReviewId}", id);

            try
            {
                Review review = GetReviewByIdInternal(id);
                
                if (review == null)
                {
                    _logger.LogWarning("API: Review not found for ID {ReviewId}", id);
                    return NotFound();
                }
                
                return Ok(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting review by ID {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retreiving review data.");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "reviewer,admin")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }
            
            _logger.LogInformation("API: Attempting to delete review {ReviewId} by User {UserId}", id, authenticatedUserId);
            
            try
            {
                Review existingReview = GetReviewByIdInternal(id);
                
                if (existingReview == null)
                {
                    _logger.LogWarning("API: Review {ReviewId} not found", id);
                    return NotFound("Review not found.");
                }
                
                bool isAuthorized = existingReview.UserID == authenticatedUserId || User.IsInRole("admin");
                if (!isAuthorized)
                {
                    _logger.LogWarning("API: User {UserId} not authorized to delete review {ReviewId}", authenticatedUserId, id);
                    return Forbid();
                }
                
                SqlCommand cmd = new SqlCommand("dbo.TP_spDeleteReview");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReviewID", id);
                cmd.Parameters.AddWithValue("@UserID", authenticatedUserId); // For ownership check in SP
                
                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                
                if (result > 0)
                {
                    _logger.LogInformation("API: Review {ReviewId} deleted successfully by User {UserId}", id, authenticatedUserId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Review {ReviewId} delete failed (DB update returned 0 rows affected).", id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete review.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error deleting review {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleteing review.");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "reviewer")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdateReviewDto reviewDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }
            
            _logger.LogInformation("API: Attempting to update review {ReviewId} by User {UserId}", id, authenticatedUserId);
            
            try
            {
                Review existingReview = GetReviewByIdInternal(id);
                
                if (existingReview == null)
                {
                    _logger.LogWarning("API: Review {ReviewId} not found", id);
                    return NotFound("Review not found.");
                }
                
                if (existingReview.UserID != authenticatedUserId)
                {
                    _logger.LogWarning("API: User {UserId} not authorized to update review {ReviewId}", authenticatedUserId, id);
                    return Forbid();
                }
                
                SqlCommand cmd = new SqlCommand("dbo.TP_spUpdateReview");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReviewID", id);
                cmd.Parameters.AddWithValue("@UserID", authenticatedUserId);
                cmd.Parameters.AddWithValue("@VisitDate", reviewDto.VisitDate);
                cmd.Parameters.AddWithValue("@Comments", reviewDto.Comments);
                cmd.Parameters.AddWithValue("@FoodQualityRating", reviewDto.FoodQualityRating);
                cmd.Parameters.AddWithValue("@ServiceRating", reviewDto.ServiceRating);
                cmd.Parameters.AddWithValue("@AtmosphereRating", reviewDto.AtmosphereRating);
                cmd.Parameters.AddWithValue("@PriceRating", reviewDto.PriceRating);
                
                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);
                
                if (result > 0)
                {
                    _logger.LogInformation("API: Review {ReviewId} updated successfully by User {UserId}", id, authenticatedUserId);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Review {ReviewId} update failed (DB update returned 0 rows affected).", id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update review.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error updating review {ReviewId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updateing review.");
            }
        }

        private Review GetReviewByIdInternal(int id)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReviewById");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReviewID", id);
                
                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return MapDataRowToReview(ds.Tables[0].Rows[0]);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Internal: Error retrieving review by ID {ReviewId}", id);
                return null;
            }
        }

        private Review MapDataRowToReview(DataRow dr)
        {
            if (dr == null) return null;
            
            try
            {
                var review = new Review
                {
                    ReviewID = Convert.ToInt32(dr["ReviewID"]),
                    RestaurantID = Convert.ToInt32(dr["RestaurantID"]),
                    UserID = Convert.ToInt32(dr["UserID"]),
                    Username = dr["Username"]?.ToString(),
                    VisitDate = Convert.ToDateTime(dr["VisitDate"]),
                    Comments = dr["Comments"]?.ToString(),
                    FoodQualityRating = Convert.ToInt32(dr["FoodQualityRating"]),
                    ServiceRating = Convert.ToInt32(dr["ServiceRating"]),
                    AtmosphereRating = Convert.ToInt32(dr["AtmosphereRating"]),
                    PriceRating = Convert.ToInt32(dr["PriceRating"]),
                    CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                    ModifiedDate = dr["ModifiedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["ModifiedDate"])
                };
                
                return review;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping DataRow to Review object. Check column names and types.");
                return null;
            }
        }

        private List<Review> MapDataSetToReviewList(DataSet ds)
        {
            var reviews = new List<Review>();
            
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Review review = MapDataRowToReview(dr);
                    if (review != null)
                    {
                        reviews.Add(review);
                    }
                }
            }
            
            return reviews;
        }
    }
} 