using CommunitySportsBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CommunitySportsBooking.Controllers
{
    public class ReviewController : Controller
    {
        private readonly DatabaseHelper _database;

        public ReviewController(DatabaseHelper database)
        {
            _database = database;
        }


        // =========================================================
        // CREATE REVIEW - GET
        // Only facilities previously used by the logged-in member
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            List<Facility> facilities =
                new List<Facility>();

            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();


                string query = @"
                    SELECT DISTINCT
                        F.FacilityID,
                        F.FacilityName,
                        F.FacilityType,
                        F.Location,
                        F.Description,
                        F.Capacity,
                        F.Status
                    FROM FACILITY F
                    INNER JOIN BOOKING B
                        ON F.FacilityID = B.FacilityID
                    WHERE
                        (
                            B.MemberID = @MemberID
                            OR B.MEMBER_MemberID = @MemberID
                        )
                        AND B.BookingDate < CAST(GETDATE() AS DATE)
                        AND B.BookingStatus = 'Confirmed'
                    ORDER BY F.FacilityName";


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
                    facilities.Add(
                        new Facility
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
                        });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load facilities: "
                    + ex.Message;
            }


            ViewBag.Facilities =
                facilities;


            return View();
        }


        // =========================================================
        // CREATE REVIEW - POST
        // Member must have used the facility before reviewing
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            int facilityId,
            int rating,
            string comment)
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            // -----------------------------------------------------
            // Validate rating
            // -----------------------------------------------------

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] =
                    "Please select a rating between 1 and 5.";

                return RedirectToAction("Create");
            }


            // -----------------------------------------------------
            // Validate comment
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] =
                    "Please enter a comment.";

                return RedirectToAction("Create");
            }


            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();


                // =================================================
                // CHECK MEMBER USED FACILITY
                // =================================================

                string bookingQuery = @"
                    SELECT COUNT(*)
                    FROM BOOKING
                    WHERE
                        (
                            MemberID = @MemberID
                            OR MEMBER_MemberID = @MemberID
                        )
                        AND
                        (
                            FacilityID = @FacilityID
                            OR FACILITY_FacilityID = @FacilityID
                        )
                        AND BookingDate < CAST(GETDATE() AS DATE)
                        AND BookingStatus = 'Confirmed'";


                using SqlCommand bookingCommand =
                    new SqlCommand(
                        bookingQuery,
                        connection);


                bookingCommand.Parameters.AddWithValue(
                    "@MemberID",
                    memberId.Value);


                bookingCommand.Parameters.AddWithValue(
                    "@FacilityID",
                    facilityId);


                int bookingCount =
                    Convert.ToInt32(
                        bookingCommand.ExecuteScalar());


                if (bookingCount == 0)
                {
                    TempData["ReviewError"] =
                        "You can only submit a review after using the facility.";

                    return RedirectToAction("Create");
                }


                // =================================================
                // CHECK FACILITY EXISTS
                // =================================================

                string facilityQuery = @"
                    SELECT COUNT(*)
                    FROM FACILITY
                    WHERE FacilityID = @FacilityID";


                using SqlCommand facilityCommand =
                    new SqlCommand(
                        facilityQuery,
                        connection);


                facilityCommand.Parameters.AddWithValue(
                    "@FacilityID",
                    facilityId);


                int facilityExists =
                    Convert.ToInt32(
                        facilityCommand.ExecuteScalar());


                if (facilityExists == 0)
                {
                    TempData["ReviewError"] =
                        "Selected facility was not found.";

                    return RedirectToAction("Create");
                }


                // =================================================
                // GENERATE REVIEW ID
                // =================================================

                string idQuery = @"
                    SELECT ISNULL(MAX(ReviewID), 0) + 1
                    FROM REVIEW";


                int reviewId;


                using SqlCommand idCommand =
                    new SqlCommand(
                        idQuery,
                        connection);


                reviewId =
                    Convert.ToInt32(
                        idCommand.ExecuteScalar());


                // =================================================
                // INSERT REVIEW
                // =================================================

                string insertQuery = @"
                    INSERT INTO REVIEW
                    (
                        ReviewID,
                        MemberID,
                        FacilityID,
                        Rating,
                        Comment,
                        ReviewDate,
                        MEMBER_MemberID,
                        FACILITY_FacilityID
                    )
                    VALUES
                    (
                        @ReviewID,
                        @MemberID,
                        @FacilityID,
                        @Rating,
                        @Comment,
                        CAST(GETDATE() AS DATE),
                        @MemberID,
                        @FacilityID
                    )";


                using SqlCommand insertCommand =
                    new SqlCommand(
                        insertQuery,
                        connection);


                insertCommand.Parameters.AddWithValue(
                    "@ReviewID",
                    reviewId);


                insertCommand.Parameters.AddWithValue(
                    "@MemberID",
                    memberId.Value);


                insertCommand.Parameters.AddWithValue(
                    "@FacilityID",
                    facilityId);


                insertCommand.Parameters.AddWithValue(
                    "@Rating",
                    rating);


                insertCommand.Parameters.AddWithValue(
                    "@Comment",
                    comment.Trim());


                insertCommand.ExecuteNonQuery();


                TempData["ReviewSuccess"] =
                    "Your review has been submitted successfully.";

                return RedirectToAction("MyReviews");
            }
            catch (Exception ex)
            {
                TempData["ReviewError"] =
                    "Unable to submit review: "
                    + ex.Message;

                return RedirectToAction("Create");
            }
        }


        // =========================================================
        // MY REVIEWS
        // =========================================================

        [HttpGet]
        public IActionResult MyReviews()
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            List<Review> reviews =
                new List<Review>();


            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();


                string query = @"
                    SELECT
                        R.ReviewID,
                        R.MemberID,
                        R.FacilityID,
                        R.Rating,
                        R.Comment,
                        R.ReviewDate,
                        F.FacilityName
                    FROM REVIEW R
                    INNER JOIN FACILITY F
                        ON R.FacilityID = F.FacilityID
                    WHERE
                        (
                            R.MemberID = @MemberID
                            OR R.MEMBER_MemberID = @MemberID
                        )
                    ORDER BY R.ReviewDate DESC";


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
                    reviews.Add(
                        new Review
                        {
                            ReviewID =
                                Convert.ToInt32(
                                    reader["ReviewID"]),

                            MemberID =
                                Convert.ToInt32(
                                    reader["MemberID"]),

                            FacilityID =
                                Convert.ToInt32(
                                    reader["FacilityID"]),

                            Rating =
                                Convert.ToInt32(
                                    reader["Rating"]),

                            Comment =
                                reader["Comment"]
                                .ToString() ?? "",

                            ReviewDate =
                                Convert.ToDateTime(
                                    reader["ReviewDate"]),

                            FacilityName =
                                reader["FacilityName"]
                                .ToString() ?? ""
                        });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load your reviews: "
                    + ex.Message;
            }


            return View(reviews);
        }


        // =========================================================
        // SEARCH REVIEWS - PUBLIC / GUEST
        // =========================================================

        [HttpGet]
        public IActionResult Search(
            string? facilityName,
            int? rating)
        {
            List<Review> reviews =
                new List<Review>();


            try
            {
                using SqlConnection connection =
                    _database.GetConnection();

                connection.Open();


                string query = @"
                    SELECT
                        R.ReviewID,
                        R.MemberID,
                        R.FacilityID,
                        R.Rating,
                        R.Comment,
                        R.ReviewDate,
                        F.FacilityName
                    FROM REVIEW R
                    INNER JOIN FACILITY F
                        ON R.FacilityID = F.FacilityID
                    WHERE
                        (
                            @FacilityName IS NULL
                            OR @FacilityName = ''
                            OR F.FacilityName LIKE
                               '%' + @FacilityName + '%'
                        )
                        AND
                        (
                            @Rating IS NULL
                            OR R.Rating = @Rating
                        )
                    ORDER BY R.ReviewDate DESC";


                using SqlCommand command =
                    new SqlCommand(
                        query,
                        connection);


                command.Parameters.AddWithValue(
                    "@FacilityName",
                    string.IsNullOrWhiteSpace(facilityName)
                        ? (object)DBNull.Value
                        : facilityName);


                command.Parameters.AddWithValue(
                    "@Rating",
                    rating.HasValue
                        ? (object)rating.Value
                        : DBNull.Value);


                using SqlDataReader reader =
                    command.ExecuteReader();


                while (reader.Read())
                {
                    reviews.Add(
                        new Review
                        {
                            ReviewID =
                                Convert.ToInt32(
                                    reader["ReviewID"]),

                            MemberID =
                                Convert.ToInt32(
                                    reader["MemberID"]),

                            FacilityID =
                                Convert.ToInt32(
                                    reader["FacilityID"]),

                            Rating =
                                Convert.ToInt32(
                                    reader["Rating"]),

                            Comment =
                                reader["Comment"]
                                .ToString() ?? "",

                            ReviewDate =
                                Convert.ToDateTime(
                                    reader["ReviewDate"]),

                            FacilityName =
                                reader["FacilityName"]
                                .ToString() ?? ""
                        });
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to load reviews: "
                    + ex.Message;
            }


            ViewBag.FacilityName =
                facilityName;

            ViewBag.Rating =
                rating;

           
            return View(reviews);
        }
    }
}