using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CommunitySportsBooking.Models;

namespace CommunitySportsBooking.Controllers
{
    public class FacilityController : Controller
    {
        private readonly DatabaseHelper _database;

        public FacilityController(DatabaseHelper database)
        {
            _database = database;
        }


        // =====================================================
        // MEMBER FACILITY SEARCH
        // =====================================================

        [HttpGet]
        public IActionResult Search(string? facilityType, string? location)
        {
            var facilities = new List<Facility>();

            try
            {
                using var connection = _database.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT
                        FacilityID,
                        FacilityName,
                        FacilityType,
                        Location,
                        Description,
                        Capacity,
                        Status
                    FROM FACILITY
                    WHERE 1 = 1";

                if (!string.IsNullOrWhiteSpace(facilityType))
                {
                    sql += " AND FacilityType LIKE @FacilityType";
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    sql += " AND Location LIKE @Location";
                }

                sql += " ORDER BY FacilityName";

                using var command = new SqlCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(facilityType))
                {
                    command.Parameters.AddWithValue(
                        "@FacilityType",
                        "%" + facilityType + "%");
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    command.Parameters.AddWithValue(
                        "@Location",
                        "%" + location + "%");
                }

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    facilities.Add(new Facility
                    {
                        FacilityID = Convert.ToInt32(reader["FacilityID"]),

                        FacilityName =
                            reader["FacilityName"].ToString() ?? "",

                        FacilityType =
                            reader["FacilityType"].ToString() ?? "",

                        Location =
                            reader["Location"].ToString() ?? "",

                        Description =
                            reader["Description"] == DBNull.Value
                                ? ""
                                : reader["Description"].ToString() ?? "",

                        Capacity =
                            Convert.ToInt32(reader["Capacity"]),

                        Status =
                            reader["Status"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load facilities: " + ex.Message;
            }

            return View(facilities);
        }


        // =====================================================
        // GUEST RESTRICTED SEARCH
        // =====================================================

        [HttpGet]
        public IActionResult GuestSearch(
            string? facilityType,
            string? location)
        {
            var facilities = new List<Facility>();

            try
            {
                using var connection = _database.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT
                        FacilityID,
                        FacilityName,
                        FacilityType,
                        Location,
                        Description,
                        Capacity,
                        Status
                    FROM FACILITY
                    WHERE 1 = 1";

                if (!string.IsNullOrWhiteSpace(facilityType))
                {
                    sql += " AND FacilityType LIKE @FacilityType";
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    sql += " AND Location LIKE @Location";
                }

                sql += " ORDER BY FacilityName";

                using var command = new SqlCommand(sql, connection);

                if (!string.IsNullOrWhiteSpace(facilityType))
                {
                    command.Parameters.AddWithValue(
                        "@FacilityType",
                        "%" + facilityType + "%");
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    command.Parameters.AddWithValue(
                        "@Location",
                        "%" + location + "%");
                }

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    facilities.Add(new Facility
                    {
                        FacilityID = Convert.ToInt32(reader["FacilityID"]),

                        FacilityName =
                            reader["FacilityName"].ToString() ?? "",

                        FacilityType =
                            reader["FacilityType"].ToString() ?? "",

                        Location =
                            reader["Location"].ToString() ?? "",

                        Description =
                            reader["Description"] == DBNull.Value
                                ? ""
                                : reader["Description"].ToString() ?? "",

                        Capacity =
                            Convert.ToInt32(reader["Capacity"]),

                        Status =
                            reader["Status"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load facilities: " + ex.Message;
            }

            return View(facilities);
        }


        // =====================================================
        // FACILITY DETAILS
        // =====================================================

        [HttpGet]
        public IActionResult Details(int id)
        {
            Facility? facility = null;

            try
            {
                using var connection = _database.GetConnection();
                connection.Open();

                string sql = @"
                    SELECT
                        FacilityID,
                        FacilityName,
                        FacilityType,
                        Location,
                        Description,
                        Capacity,
                        Status
                    FROM FACILITY
                    WHERE FacilityID = @FacilityID";

                using var command = new SqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@FacilityID",
                    id);

                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    facility = new Facility
                    {
                        FacilityID =
                            Convert.ToInt32(reader["FacilityID"]),

                        FacilityName =
                            reader["FacilityName"].ToString() ?? "",

                        FacilityType =
                            reader["FacilityType"].ToString() ?? "",

                        Location =
                            reader["Location"].ToString() ?? "",

                        Description =
                            reader["Description"] == DBNull.Value
                                ? ""
                                : reader["Description"].ToString() ?? "",

                        Capacity =
                            Convert.ToInt32(reader["Capacity"]),

                        Status =
                            reader["Status"].ToString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load facility details: " + ex.Message;
            }

            if (facility == null)
            {
                return NotFound();
            }

            return View(facility);
        }
    }
}
