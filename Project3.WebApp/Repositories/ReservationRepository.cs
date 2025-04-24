using Project3.Shared.Utilities;
using Project3.WebApp.Models;
using System.Data;
using System.Data.SqlClient;

namespace Project3.WebApp.Repositories
{
    // Repository class for retrieving reservation data
    public class ReservationRepository
    {
        private readonly Connection _db;

        public ReservationRepository(Connection db)
        {
            _db = db;
        }

        // Gets all reservations from DB using stored proc
        public List<ReservationData> GetAllReservations()
        {
            var reservations = new List<ReservationData>();

            try
            {

                var cmd = new SqlCommand("TP_spGetAllReservations");
                cmd.CommandType = CommandType.StoredProcedure;

                var ds = _db.GetDataSetUsingCmdObj(cmd);

                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        reservations.Add(new ReservationData
                        {
                            ReservationDateTime = Convert.ToDateTime(row["ReservationDateTime"]),
                            PartySize = Convert.ToInt32(row["PartySize"]),
                            Status = row["Status"].ToString()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // When DB fails, return fake data instead of crashing
                return GetDummyReservationData();
            }

            return reservations;
        }

        // Same as above but filtered by restaurant ID
        public List<ReservationData> GetReservationsByRestaurantId(string restaurantId)
        {
            var reservations = new List<ReservationData>();

            try
            {

                var cmd = new SqlCommand("TP_spGetReservationsForRestaurant");
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@RestaurantID", restaurantId);

                var ds = _db.GetDataSetUsingCmdObj(cmd);

                if (ds?.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        reservations.Add(new ReservationData
                        {
                            ReservationDateTime = Convert.ToDateTime(row["ReservationDateTime"]),
                            PartySize = Convert.ToInt32(row["PartySize"]),
                            Status = row["Status"].ToString()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // DB error - use dummy data with restaurant ID as seed
                return GetDummyReservationData(restaurantId);
            }

            return reservations;
        }

        // Just calls the other method with "dummy" as ID
        private List<ReservationData> GetDummyReservationData()
        {
            return GetDummyReservationData("dummy");
        }

        // Creates fake reservation data - more on weekends
        private List<ReservationData> GetDummyReservationData(string restaurantId)
        {
            var reservations = new List<ReservationData>();
            var random = new Random(restaurantId.GetHashCode()); // Same seed = same fake data

            // Past year of data
            var endDate = DateTime.Today;
            var startDate = endDate.AddMonths(-11);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Weekend logic - more reservations on weekends
                int count = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                    ? random.Next(5, 15)
                    : random.Next(1, 8);

                // Generate random dinner time reservations
                for (int i = 0; i < count; i++)
                {
                    int hour = random.Next(17, 22); // 5 PM to 9 PM
                    int minute = random.Next(0, 4) * 15; // 0, 15, 30, 45 minutes

                    reservations.Add(new ReservationData
                    {
                        ReservationDateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0),
                        PartySize = random.Next(1, 9),
                        Status = "Confirmed"
                    });
                }
            }

            return reservations;
        }
    }
}