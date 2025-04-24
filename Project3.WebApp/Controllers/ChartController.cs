// controller for displaying charts dashboard, only for restaurant reps
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Utilities;
using Project3.WebApp.Models;
using Project3.WebApp.Repositories;
using Project3.WebApp.Services;
using System.Security.Claims;
using System.Data;
using System.Data.SqlClient;

namespace Project3.WebApp.Controllers
{
    [Authorize(Roles = "RestaurantRep")]
    public class ChartController : Controller
    {
        private readonly Connection _db;
        private readonly ChartService _chartService;
        private readonly ILogger<ChartController> _logger;
        private readonly ReservationRepository _reservationRepository;

        public ChartController(
            Connection db,
            ChartService chartService,
            ILogger<ChartController> logger,
            ReservationRepository reservationRepository)
        {
            _db = db;
            _chartService = chartService;
            _logger = logger;
            _reservationRepository = reservationRepository;
        }

        // shows the main charts page with reservation analytics
        public IActionResult Index()
        {
            var restaurantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(restaurantId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["RestaurantId"] = restaurantId;
            return View();
        }

        // returns bar chart of reservations by day of week
        [HttpGet]
        public IActionResult ReservationsByDay()
        {
            try
            {
                var restaurantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(restaurantId))
                {
                    return StatusCode(401); // unauthorized
                }

                // fetch reservations for this restaurant
                var reservations = _reservationRepository.GetReservationsByRestaurantId(restaurantId);
                // calculate counts per day
                var reservationCounts = ChartDataGenerator.GetReservationsByDayOfWeek(reservations);
                // generate bar chart
                byte[] chartBytes = _chartService.GenerateBarChart(reservationCounts, "Reservations by Day of Week");
                return File(chartBytes, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating reservations chart: {ex.Message}");
                return StatusCode(500, "Error generating chart");
            }
        }

        // returns line chart for reservation trend over last 12 months
        [HttpGet]
        public IActionResult ReservationTrend()
        {
            try
            {
                var restaurantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(restaurantId))
                {
                    return StatusCode(401);
                }

                var reservations = _reservationRepository.GetReservationsByRestaurantId(restaurantId);
                var monthCounts = ChartDataGenerator.GetReservationTrend(reservations);
                byte[] chartBytes = _chartService.GenerateLineChart(monthCounts, "Reservation Trend (Last 12 Months)");
                return File(chartBytes, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating reservation trend chart: {ex.Message}");
                return StatusCode(500, "Error generating chart");
            }
        }

    }
}