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
    public class ReservationsApiController : ControllerBase
    {
        private readonly ILogger<ReservationsApiController> _logger;
        private readonly Connection _dbConnect;

        public ReservationsApiController(
            ILogger<ReservationsApiController> logger, 
            Connection dbConnect)
        {
            _logger = logger;
            _dbConnect = dbConnect;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<Reservation>> CreateReservation([FromBody] CreateReservationDto reservationDto)
        {
            _logger.LogInformation("API: Attempting to create reservation for Restaurant {RestaurantId}", reservationDto.RestaurantID);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int parsedUserId)) { userId = parsedUserId; }
                else { _logger.LogWarning("API: Could not parse UserID from authenticated user claims."); }
            }

            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spAddReservation");
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RestaurantID", reservationDto.RestaurantID);
                cmd.Parameters.AddWithValue("@UserID", userId.HasValue ? (object)userId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@ReservationDateTime", reservationDto.ReservationDateTime);
                cmd.Parameters.AddWithValue("@PartySize", reservationDto.PartySize);
                cmd.Parameters.AddWithValue("@ContactName", reservationDto.ContactName);
                cmd.Parameters.AddWithValue("@Phone", reservationDto.Phone);
                cmd.Parameters.AddWithValue("@Email", reservationDto.Email);
                cmd.Parameters.AddWithValue("@SpecialRequests", string.IsNullOrEmpty(reservationDto.SpecialRequests) ? DBNull.Value : reservationDto.SpecialRequests);
                cmd.Parameters.AddWithValue("@Status", "Pending");

                object result = _dbConnect.ExecuteScalarUsingCmdObj(cmd);
                int newReservationId = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

                if (newReservationId > 0)
                {
                    _logger.LogInformation("API: Reservation {ReservationId} created for Restaurant {RestaurantId}", newReservationId, reservationDto.RestaurantID);
                    Reservation createdReservation = GetReservationByIdInternal(newReservationId);
                    if (createdReservation != null)
                    {
                        return CreatedAtAction(nameof(GetReservationById), new { id = newReservationId }, createdReservation);
                    }
                }
                else
                {
                    _logger.LogWarning("API: Stored procedure executed but didn't return a reservation ID. Checking for recently added reservations...");
                    
                    try
                    {
                        SqlCommand recentCmd = new SqlCommand("SELECT TOP 1 ReservationID FROM TP_Reservations WHERE RestaurantID = @RestaurantID AND ContactName = @ContactName ORDER BY CreatedDate DESC");
                        recentCmd.Parameters.AddWithValue("@RestaurantID", reservationDto.RestaurantID);
                        recentCmd.Parameters.AddWithValue("@ContactName", reservationDto.ContactName);
                        
                        object recentResult = _dbConnect.ExecuteScalarUsingCmdObj(recentCmd);
                        int recentId = (recentResult != null && recentResult != DBNull.Value) ? Convert.ToInt32(recentResult) : 0;
                        
                        if (recentId > 0)
                        {
                            _logger.LogInformation("API: Found recently created reservation {ReservationId} for Restaurant {RestaurantId}", recentId, reservationDto.RestaurantID);
                            Reservation recentReservation = GetReservationByIdInternal(recentId);
                            
                            if (recentReservation != null)
                            {
                                return CreatedAtAction(nameof(GetReservationById), new { id = recentId }, recentReservation);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "API: Error trying to retrieve recently created reservation for Restaurant {RestaurantId}", reservationDto.RestaurantID);
                    }
                    
                    _logger.LogError("API: Failed to add reservation record to DB (ExecuteScalar returned 0 or null) for Restaurant {RestaurantId}", reservationDto.RestaurantID);
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error saving reservation data.");
                }
                
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving created reservation.");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error creating reservation for Restaurant {RestaurantId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    reservationDto.RestaurantID, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error creating reservation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error creating reservation for Restaurant {RestaurantId}", reservationDto.RestaurantID);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating reservation.");
            }
        }

        [HttpGet("{id:int}", Name = nameof(GetReservationById))]
        [Authorize]
        public async Task<ActionResult<Reservation>> GetReservationById(int id)
        {
            _logger.LogInformation("API: Getting reservation by ID {ReservationId}", id);
            try
            {
                Reservation reservation = GetReservationByIdInternal(id);
                if (reservation == null) { return NotFound(); }
                return Ok(reservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting reservation by ID {ReservationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retreiving reservation data.");
            }
        }

        [HttpGet("restaurant/{restaurantId:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservationsForRestaurant(int restaurantId, [FromQuery] string? status)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            _logger.LogInformation("API: Getting reservations for Restaurant ID {RestaurantId} with Status={Status} by Rep {RepUserId}", restaurantId, status ?? "All", authenticatedUserId);
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReservationsByRestaurant");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);
                cmd.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(status) ? (object)DBNull.Value : status);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);
                List<Reservation> reservations = MapDataSetToReservationList(ds);

                return Ok(reservations);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error getting reservations for Restaurant ID {RestaurantId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    restaurantId, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error retreiving reservations.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error getting reservations for Restaurant ID {RestaurantId}", restaurantId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retreiving reservations.");
            }
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            _logger.LogInformation("API: Attempting to update status for reservation {ReservationId} to {NewStatus} by Rep {UserId}", id, statusDto.Status, authenticatedUserId);

            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                Reservation existingReservation = GetReservationByIdInternal(id);
                if (existingReservation == null)
                {
                    return NotFound("Reservation not found.");
                }

                SqlCommand cmd = new SqlCommand("dbo.TP_spUpdateReservationStatus");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);
                cmd.Parameters.AddWithValue("@NewStatus", statusDto.Status);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Reservation {ReservationId} status updated to {NewStatus} by Rep {UserId}", id, statusDto.Status, authenticatedUserId);
                    
                    existingReservation.Status = statusDto.Status;
                    
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Reservation status update failed for ID {ReservationId} (Update returned 0 rows affected - check ownership/existence).", id);
                    return NotFound("Reservation not found or update failled.");
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error updating status for reservation ID {ReservationId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    id, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error updateing reservation status.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error updating status for reservation ID {ReservationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating reservation status.");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "restaurantRep")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var authenticatedUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(authenticatedUserIdString, out int authenticatedUserId))
            { return Unauthorized("User identifier claim is invalid or missing."); }

            _logger.LogInformation("API: Attempting to delete reservation {ReservationId} by Rep {UserId}", id, authenticatedUserId);

            try
            {
                Reservation existingReservation = GetReservationByIdInternal(id);
                if (existingReservation == null)
                {
                    return NotFound("Reservation not found.");
                }

                SqlCommand cmd = new SqlCommand("dbo.TP_spDeleteReservation");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);

                int result = _dbConnect.DoUpdateUsingCmdObj(cmd);

                if (result > 0)
                {
                    _logger.LogInformation("API: Reservation {ReservationId} deleted successfully by Rep {UserId}", id, authenticatedUserId);
                    
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("API: Reservation delete failed for ID {ReservationId} (Update returned 0 rows affected - check ownership/existence).", id);
                    return NotFound("Reservation not found or delete failled.");
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "API: SQL Error deleting reservation ID {ReservationId}. Error Number: {ErrorNumber}. Message: {ErrorMessage}",
                    id, sqlEx.Number, sqlEx.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Database error deleteing reservation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: General Error deleting reservation ID {ReservationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting reservation.");
            }
        }

        private Reservation GetReservationByIdInternal(int id)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("dbo.TP_spGetReservationById");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationID", id);

                DataSet ds = _dbConnect.GetDataSetUsingCmdObj(cmd);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return MapDataRowToReservation(ds.Tables[0].Rows[0]);
                }
                else
                {
                    _logger.LogWarning("API Internal: Reservation not found using stored procedure, trying direct SQL");
                    
                    SqlCommand directCmd = new SqlCommand("SELECT * FROM TP_Reservations WHERE ReservationID = @ReservationID");
                    directCmd.Parameters.AddWithValue("@ReservationID", id);
                    
                    DataSet directDs = _dbConnect.GetDataSetUsingCmdObj(directCmd);
                    
                    if (directDs != null && directDs.Tables.Count > 0 && directDs.Tables[0].Rows.Count > 0)
                    {
                        return MapDataRowToReservation(directDs.Tables[0].Rows[0]);
                    }
                    
                    _logger.LogWarning("API Internal: Reservation not found for ID {ReservationId}", id);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Internal: Error retrieving reservation by ID {ReservationId}, trying direct SQL", id);
                
                try {
                    SqlCommand directCmd = new SqlCommand("SELECT * FROM TP_Reservations WHERE ReservationID = @ReservationID");
                    directCmd.Parameters.AddWithValue("@ReservationID", id);
                    
                    DataSet directDs = _dbConnect.GetDataSetUsingCmdObj(directCmd);
                    
                    if (directDs != null && directDs.Tables.Count > 0 && directDs.Tables[0].Rows.Count > 0)
                    {
                        return MapDataRowToReservation(directDs.Tables[0].Rows[0]);
                    }
                }
                catch (Exception innerEx) {
                    _logger.LogError(innerEx, "API Internal: Error retrieving reservation by ID {ReservationId} using direct SQL", id);
                }
                
                return null;
            }
        }

        private Reservation MapDataRowToReservation(DataRow dr)
        {
            if (dr == null) return null;
            try
            {
                var reservation = new Reservation();
                
                if (dr.Table.Columns.Contains("ReservationID"))
                    reservation.ReservationID = Convert.ToInt32(dr["ReservationID"]);
                
                if (dr.Table.Columns.Contains("RestaurantID"))
                    reservation.RestaurantID = Convert.ToInt32(dr["RestaurantID"]);
                
                if (dr.Table.Columns.Contains("UserID"))
                    reservation.UserID = dr["UserID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["UserID"]);
                
                if (dr.Table.Columns.Contains("ReservationDateTime"))
                    reservation.ReservationDateTime = Convert.ToDateTime(dr["ReservationDateTime"]);
                else
                    reservation.ReservationDateTime = DateTime.Now;
                
                if (dr.Table.Columns.Contains("PartySize"))
                    reservation.PartySize = Convert.ToInt32(dr["PartySize"]);
                else
                    reservation.PartySize = 1;
                
                if (dr.Table.Columns.Contains("ContactName"))
                    reservation.ContactName = dr["ContactName"]?.ToString();
                
                if (dr.Table.Columns.Contains("SpecialRequests"))
                    reservation.SpecialRequests = dr["SpecialRequests"]?.ToString();
                
                if (dr.Table.Columns.Contains("Status"))
                    reservation.Status = dr["Status"]?.ToString();
                else
                    reservation.Status = "Pending";
                
                if (dr.Table.Columns.Contains("CreatedDate"))
                    reservation.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                else if (dr.Table.Columns.Contains("DateCreated"))
                    reservation.CreatedDate = Convert.ToDateTime(dr["DateCreated"]);
                else if (dr.Table.Columns.Contains("CreateDate"))
                    reservation.CreatedDate = Convert.ToDateTime(dr["CreateDate"]);
                else
                    reservation.CreatedDate = DateTime.Now;
                
                if (dr.Table.Columns.Contains("Phone"))
                    if (dr["Phone"] != DBNull.Value)
                        reservation.ContactPhone = dr["Phone"].ToString();
                    
                if (dr.Table.Columns.Contains("Email"))
                    if (dr["Email"] != DBNull.Value)
                        reservation.ContactEmail = dr["Email"].ToString();
                
                return reservation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping DataRow to Reservation object. Check column names and types in MapDataRowToReservation. Error: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        private List<Reservation> MapDataSetToReservationList(DataSet ds)
        {
            var reservations = new List<Reservation>();
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Reservation res = MapDataRowToReservation(dr);
                    if (res != null) { reservations.Add(res); }
                }
            }
            return reservations;
        }
    }
} 