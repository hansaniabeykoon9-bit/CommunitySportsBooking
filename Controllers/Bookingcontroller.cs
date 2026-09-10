using CommunitySportsBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CommunitySportsBooking.Controllers
{
    public class BookingController : Controller
    {
        private readonly DatabaseHelper _database;

        public BookingController(DatabaseHelper database)
        {
            _database = database;
        }


        // =========================================================
        // CREATE BOOKING - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create(int facilityId)
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();

                string query = @"
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

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@FacilityID",
                    facilityId);

                using SqlDataReader reader =
                    command.ExecuteReader();

                if (reader.Read())
                {
                    Facility facility = new Facility
                    {
                        FacilityID =
                            Convert.ToInt32(
                                reader["FacilityID"]),

                        FacilityName =
                            reader["FacilityName"]
                            .ToString() ?? "",

                        FacilityType =
                            reader["FacilityType"]
                            .ToString() ?? "",

                        Location =
                            reader["Location"]
                            .ToString() ?? "",

                        Description =
                            reader["Description"] == DBNull.Value
                                ? ""
                                : reader["Description"]
                                .ToString() ?? "",

                        Capacity =
                            Convert.ToInt32(
                                reader["Capacity"]),

                        Status =
                            reader["Status"]
                            .ToString() ?? ""
                    };

                    // IMPORTANT:
                    // create.cshtml uses @Model.FacilityName,
                    // @Model.Status, etc.
                    return View(facility);
                }

                TempData["BookingError"] =
                    "Facility not found.";

                return RedirectToAction(
                    "Search",
                    "Facility");
            }
            catch (Exception ex)
            {
                TempData["BookingError"] =
                    "Unable to load booking page: "
                    + ex.Message;

                return RedirectToAction(
                    "Search",
                    "Facility");
            }
        }


        // =========================================================
        // CREATE BOOKING - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            int facilityId,
            DateTime bookingDate,
            string startTime,
            string endTime,
            string paymentMethod)
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // =====================================================
            // VALIDATE BOOKING DATE
            // =====================================================

            if (bookingDate.Date < DateTime.Today)
            {
                TempData["BookingError"] =
                    "Booking date cannot be in the past.";

                return RedirectToAction(
                    "Create",
                    new { facilityId });
            }


            // =====================================================
            // VALIDATE START TIME
            // =====================================================

            if (!TimeSpan.TryParse(
                    startTime,
                    out TimeSpan parsedStartTime))
            {
                TempData["BookingError"] =
                    "Please enter a valid start time.";

                return RedirectToAction(
                    "Create",
                    new { facilityId });
            }


            // =====================================================
            // VALIDATE END TIME
            // =====================================================

            if (!TimeSpan.TryParse(
                    endTime,
                    out TimeSpan parsedEndTime))
            {
                TempData["BookingError"] =
                    "Please enter a valid end time.";

                return RedirectToAction(
                    "Create",
                    new { facilityId });
            }


            // =====================================================
            // CHECK TIME ORDER
            // =====================================================

            if (parsedEndTime <= parsedStartTime)
            {
                TempData["BookingError"] =
                    "End time must be later than start time.";

                return RedirectToAction(
                    "Create",
                    new { facilityId });
            }


            // =====================================================
            // CALCULATE DURATION
            // =====================================================

            double durationHours =
                (parsedEndTime - parsedStartTime)
                .TotalHours;


            // =====================================================
            // FACILITY RATE
            // Rs. 900 PER HOUR
            // =====================================================

            decimal hourlyRate = 900m;

            decimal amount =
                (decimal)durationHours * hourlyRate;


            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();


                // =================================================
                // CHECK FACILITY EXISTS AND STATUS
                // =================================================

                string facilityQuery = @"
                    SELECT
                        FacilityName,
                        Status
                    FROM FACILITY
                    WHERE FacilityID = @FacilityID";

                string facilityName = "";
                string facilityStatus = "";

                using (SqlCommand command =
                       new SqlCommand(
                           facilityQuery,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@FacilityID",
                        facilityId);

                    using SqlDataReader reader =
                        command.ExecuteReader();

                    if (!reader.Read())
                    {
                        TempData["BookingError"] =
                            "Facility was not found.";

                        return RedirectToAction(
                            "Search",
                            "Facility");
                    }

                    facilityName =
                        reader["FacilityName"]
                        .ToString() ?? "";

                    facilityStatus =
                        reader["Status"]
                        .ToString() ?? "";
                }


                if (!facilityStatus.Equals(
                        "Available",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["BookingError"] =
                        "This facility is currently unavailable.";

                    return RedirectToAction(
                        "Create",
                        new { facilityId });
                }


                // =================================================
                // CHECK BOOKING CONFLICT
                // =================================================

                string conflictQuery = @"
                    SELECT COUNT(*)
                    FROM BOOKING
                    WHERE FacilityID = @FacilityID
                      AND BookingDate = @BookingDate
                      AND BookingStatus IN
                          ('Confirmed', 'Pending')
                      AND @StartTime < EndTime
                      AND @EndTime > StartTime";

                using (SqlCommand command =
                       new SqlCommand(
                           conflictQuery,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@FacilityID",
                        facilityId);

                    command.Parameters.AddWithValue(
                        "@BookingDate",
                        bookingDate.Date);

                    command.Parameters.AddWithValue(
                        "@StartTime",
                        parsedStartTime);

                    command.Parameters.AddWithValue(
                        "@EndTime",
                        parsedEndTime);

                    int conflictCount =
                        Convert.ToInt32(
                            command.ExecuteScalar());

                    if (conflictCount > 0)
                    {
                        TempData["BookingError"] =
                            "The selected time is already booked. "
                            + "Please choose another time.";

                        return RedirectToAction(
                            "Create",
                            new { facilityId });
                    }
                }


                // =================================================
                // GENERATE PAYMENT ID
                // =================================================

                string paymentIdQuery = @"
                    SELECT ISNULL(MAX(PaymentID), 0) + 1
                    FROM PAYMENT";

                int paymentId;

                using (SqlCommand command =
                       new SqlCommand(
                           paymentIdQuery,
                           connection))
                {
                    paymentId =
                        Convert.ToInt32(
                            command.ExecuteScalar());
                }


                // =================================================
                // GENERATE BOOKING ID
                // =================================================

                string bookingIdQuery = @"
                    SELECT ISNULL(MAX(BookingID), 0) + 1
                    FROM BOOKING";

                int bookingId;

                using (SqlCommand command =
                       new SqlCommand(
                           bookingIdQuery,
                           connection))
                {
                    bookingId =
                        Convert.ToInt32(
                            command.ExecuteScalar());
                }


                // =================================================
                // PAYMENT STATUS
                // =================================================

                string paymentStatus;

                if (string.Equals(
                        paymentMethod,
                        "Cash",
                        StringComparison.OrdinalIgnoreCase))
                {
                    paymentStatus = "Pending";
                }
                else
                {
                    paymentStatus = "Paid";
                }


                // =================================================
                // BOOKING STATUS
                // =================================================

                string bookingStatus;

                if (string.Equals(
                        paymentMethod,
                        "Cash",
                        StringComparison.OrdinalIgnoreCase))
                {
                    bookingStatus = "Pending";
                }
                else
                {
                    bookingStatus = "Confirmed";
                }


                // =================================================
                // TRANSACTION REFERENCE
                // =================================================

                string transactionReference =
                    "TXN" +
                    DateTime.Now.ToString(
                        "yyyyMMddHHmmss");


                // =================================================
                // INSERT PAYMENT
                // =================================================

                string paymentInsertQuery = @"
                    INSERT INTO PAYMENT
                    (
                        PaymentID,
                        BookingID,
                        PaymentDate,
                        Amount,
                        PaymentMethod,
                        PaymentStatus,
                        TransactionReference
                    )
                    VALUES
                    (
                        @PaymentID,
                        @BookingID,
                        CAST(GETDATE() AS DATE),
                        @Amount,
                        @PaymentMethod,
                        @PaymentStatus,
                        @TransactionReference
                    )";

                using (SqlCommand command =
                       new SqlCommand(
                           paymentInsertQuery,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@PaymentID",
                        paymentId);

                    command.Parameters.AddWithValue(
                        "@BookingID",
                        bookingId);

                    command.Parameters.AddWithValue(
                        "@Amount",
                        amount);

                    command.Parameters.AddWithValue(
                        "@PaymentMethod",
                        paymentMethod);

                    command.Parameters.AddWithValue(
                        "@PaymentStatus",
                        paymentStatus);

                    command.Parameters.AddWithValue(
                        "@TransactionReference",
                        transactionReference);

                    command.ExecuteNonQuery();
                }


                // =================================================
                // INSERT BOOKING
                // =================================================

                string bookingInsertQuery = @"
                    INSERT INTO BOOKING
                    (
                        BookingID,
                        MemberID,
                        FacilityID,
                        BookingDate,
                        StartTime,
                        EndTime,
                        BookingStatus,
                        MEMBER_MemberID,
                        FACILITY_FacilityID,
                        PAYMENT_PaymentID
                    )
                    VALUES
                    (
                        @BookingID,
                        @MemberID,
                        @FacilityID,
                        @BookingDate,
                        @StartTime,
                        @EndTime,
                        @BookingStatus,
                        @MemberID,
                        @FacilityID,
                        @PaymentID
                    )";

                using (SqlCommand command =
                       new SqlCommand(
                           bookingInsertQuery,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@BookingID",
                        bookingId);

                    command.Parameters.AddWithValue(
                        "@MemberID",
                        memberId.Value);

                    command.Parameters.AddWithValue(
                        "@FacilityID",
                        facilityId);

                    command.Parameters.AddWithValue(
                        "@BookingDate",
                        bookingDate.Date);

                    command.Parameters.AddWithValue(
                        "@StartTime",
                        parsedStartTime);

                    command.Parameters.AddWithValue(
                        "@EndTime",
                        parsedEndTime);

                    command.Parameters.AddWithValue(
                        "@BookingStatus",
                        bookingStatus);

                    command.Parameters.AddWithValue(
                        "@PaymentID",
                        paymentId);

                    command.ExecuteNonQuery();
                }


                // =================================================
                // SUCCESS MESSAGE
                // =================================================

                TempData["SuccessMessage"] =
                    "Booking successful for "
                    + facilityName
                    + ". Total amount: Rs. "
                    + amount.ToString("N2");

                return RedirectToAction(
                    "MyBookings");
            }
            catch (Exception ex)
            {
                TempData["BookingError"] =
                    "Unable to create booking: "
                    + ex.Message;

                return RedirectToAction(
                    "Create",
                    new { facilityId });
            }
        }


        // =========================================================
        // MY BOOKINGS
        // =========================================================

        [HttpGet]
        public IActionResult MyBookings()
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            List<Booking> bookings =
                new List<Booking>();

            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();

                string query = @"
                    SELECT
                        B.BookingID,
                        B.MemberID,
                        B.FacilityID,
                        B.BookingDate,
                        B.StartTime,
                        B.EndTime,
                        B.BookingStatus,
                        F.FacilityName,
                        P.Amount,
                        P.PaymentID
                    FROM BOOKING B
                    INNER JOIN FACILITY F
                        ON B.FacilityID = F.FacilityID
                    LEFT JOIN PAYMENT P
                        ON B.PAYMENT_PaymentID = P.PaymentID
                    WHERE B.MemberID = @MemberID
                    ORDER BY
                        B.BookingDate DESC,
                        B.StartTime DESC";

                using SqlCommand command =
                    new SqlCommand(
                        query,
                        connection);

                command.Parameters.AddWithValue(
                    "@MemberID",
                    memberId.Value);

                using SqlDataReader reader =
                    command.ExecuteReader();

                while (reader.Read())
                {
                    bookings.Add(
                        new Booking
                        {
                            BookingID =
                                Convert.ToInt32(
                                    reader["BookingID"]),

                            MemberID =
                                Convert.ToInt32(
                                    reader["MemberID"]),

                            FacilityID =
                                Convert.ToInt32(
                                    reader["FacilityID"]),

                            BookingDate =
                                Convert.ToDateTime(
                                    reader["BookingDate"]),

                            StartTime =
                                (TimeSpan)
                                reader["StartTime"],

                            EndTime =
                                (TimeSpan)
                                reader["EndTime"],

                            BookingStatus =
                                reader["BookingStatus"]
                                .ToString() ?? "",

                            FacilityName =
                                reader["FacilityName"]
                                .ToString() ?? "",

                            Amount =
                                reader["Amount"] == DBNull.Value
                                    ? 0m
                                    : Convert.ToDecimal(
                                        reader["Amount"]),

                            PaymentID =
                                reader["PaymentID"] ==
                                DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        reader["PaymentID"])
                        });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load bookings: "
                    + ex.Message;
            }

            return View(bookings);
        }
    }
}