namespace Project3.WebApp.Models
{
    // Model for a reservation
    public class ReservationData
    {
        public DateTime ReservationDateTime { get; set; }
        public int PartySize { get; set; }
        public string? Status { get; set; }
    }

    // Model containing methods to generate chart data from reservation collection
    public static class ChartDataGenerator
    {
        // Generate data for the reservations by day of week chart
        public static Dictionary<string, int> GetReservationsByDayOfWeek(IEnumerable<ReservationData> reservations)
        {
            var dayNames = new Dictionary<int, string>
            {
                { 0, "Sunday" },
                { 1, "Monday" },
                { 2, "Tuesday" },
                { 3, "Wednesday" },
                { 4, "Thursday" },
                { 5, "Friday" },
                { 6, "Saturday" }
            };

            var reservationCounts = new Dictionary<string, int>();
            
            // Initialize all days to 0
            foreach (var day in dayNames.Values)
            {
                reservationCounts.Add(day, 0);
            }

            // Group reservations by day of week and count them
            var groupedReservations = reservations
                .GroupBy(r => ((int)r.ReservationDateTime.DayOfWeek))
                .Select(g => new { DayOfWeek = g.Key, Count = g.Count() });

            // Update counts
            foreach (var group in groupedReservations)
            {
                if (dayNames.TryGetValue(group.DayOfWeek, out string? dayName) && dayName != null)
                {
                    reservationCounts[dayName] = group.Count;
                }
            }

            // Sort by day of week
            return reservationCounts
                .OrderBy(kv => Array.IndexOf(dayNames.Values.ToArray(), kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        // Generate data for the reservation trend over time chart
        public static Dictionary<string, int> GetReservationTrend(IEnumerable<ReservationData> reservations)
        {
            // Get current date and date 12 months ago
            var endDate = DateTime.Today;
            var startDate = endDate.AddMonths(-11);
            
            // Create a dictionary with all months, initialized to 0
            var monthCounts = new Dictionary<string, int>();
            for (int i = 0; i < 12; i++)
            {
                var month = startDate.AddMonths(i);
                monthCounts.Add(month.ToString("MMM yyyy"), 0);
            }

            // Filter reservations to the last 12 months
            var relevantReservations = reservations
                .Where(r => r.ReservationDateTime >= startDate && r.ReservationDateTime <= endDate);

            // Group reservations by month and count them
            var groupedReservations = relevantReservations
                .GroupBy(r => r.ReservationDateTime.ToString("MMM yyyy"))
                .Select(g => new { Month = g.Key, Count = g.Count() });

            // Update counts
            foreach (var group in groupedReservations)
            {
                if (monthCounts.ContainsKey(group.Month))
                {
                    monthCounts[group.Month] = group.Count;
                }
            }

            // The dictionary is already sorted chronologically since we created it that way
            return monthCounts;
        }
    }
} 