using CommunitySportsBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace CommunitySportsBooking.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseHelper _database;

        public AccountController(DatabaseHelper database)
        {
            _database = database;
        }


        // =====================================================
        // REGISTER - GET
        // =====================================================

        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterViewModel();

            model.Sports = GetSports();

            return View(model);
        }


        // =====================================================
        // REGISTER - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            model.Sports = GetSports();

            if (model.SelectedSports == null ||
                model.SelectedSports.Count == 0)
            {
                ModelState.AddModelError(
                    "SelectedSports",
                    "Please select at least one preferred sport.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (var connection = _database.GetConnection())
                {
                    connection.Open();

                    string checkEmailSql = @"
                        SELECT COUNT(*)
                        FROM MEMBER
                        WHERE Email = @Email";

                    using (var command =
                           new SqlCommand(checkEmailSql, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@Email",
                            model.Email.Trim());

                        int count =
                            Convert.ToInt32(
                                command.ExecuteScalar());

                        if (count > 0)
                        {
                            ModelState.AddModelError(
                                "Email",
                                "This email is already registered.");

                            return View(model);
                        }
                    }


                    using (var transaction =
                           connection.BeginTransaction())
                    {
                        try
                        {
                            string memberIdSql = @"
                                SELECT ISNULL(MAX(MemberID), 0) + 1
                                FROM MEMBER";

                            int memberId;

                            using (var command =
                                   new SqlCommand(
                                       memberIdSql,
                                       connection,
                                       transaction))
                            {
                                memberId =
                                    Convert.ToInt32(
                                        command.ExecuteScalar());
                            }


                            string passwordHash =
                                HashPassword(model.Password);


                            string memberSql = @"
                                INSERT INTO MEMBER
                                (
                                    MemberID,
                                    FullName,
                                    Email,
                                    PasswordHash,
                                    Phone,
                                    Address
                                )
                                VALUES
                                (
                                    @MemberID,
                                    @FullName,
                                    @Email,
                                    @PasswordHash,
                                    @Phone,
                                    @Address
                                )";


                            using (var command =
                                   new SqlCommand(
                                       memberSql,
                                       connection,
                                       transaction))
                            {
                                command.Parameters.AddWithValue(
                                    "@MemberID",
                                    memberId);

                                command.Parameters.AddWithValue(
                                    "@FullName",
                                    model.FullName.Trim());

                                command.Parameters.AddWithValue(
                                    "@Email",
                                    model.Email.Trim());

                                command.Parameters.AddWithValue(
                                    "@PasswordHash",
                                    passwordHash);

                                command.Parameters.AddWithValue(
                                    "@Phone",
                                    model.Phone.Trim());

                                command.Parameters.AddWithValue(
                                    "@Address",
                                    model.Address.Trim());

                                command.ExecuteNonQuery();
                            }


                            foreach (int sportId in model.SelectedSports)
                            {
                                string sportSql = @"
                                    INSERT INTO MEMBER_SPORT
                                    (
                                        MemberID,
                                        SportID,
                                        MEMBER_MemberID,
                                        SPORT_SportID
                                    )
                                    VALUES
                                    (
                                        @MemberID,
                                        @SportID,
                                        @MemberID,
                                        @SportID
                                    )";

                                using (var command =
                                       new SqlCommand(
                                           sportSql,
                                           connection,
                                           transaction))
                                {
                                    command.Parameters.AddWithValue(
                                        "@MemberID",
                                        memberId);

                                    command.Parameters.AddWithValue(
                                        "@SportID",
                                        sportId);

                                    command.ExecuteNonQuery();
                                }
                            }


                            transaction.Commit();

                            TempData["SuccessMessage"] =
                                "Registration successful! Please sign in.";

                            return RedirectToAction("Login");
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Registration failed: " + ex.Message);

                return View(model);
            }
        }


        // =====================================================
        // LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // =====================================================
        // LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (var connection =
                       _database.GetConnection())
                {
                    connection.Open();

                    string sql = @"
                        SELECT
                            MemberID,
                            FullName,
                            PasswordHash
                        FROM MEMBER
                        WHERE Email = @Email";

                    using (var command =
                           new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@Email",
                            model.Email.Trim());

                        using (var reader =
                               command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int memberId =
                                    Convert.ToInt32(
                                        reader["MemberID"]);

                                string fullName =
                                    reader["FullName"]
                                    .ToString() ?? "";

                                string storedHash =
                                    reader["PasswordHash"]
                                    .ToString() ?? "";

                                string enteredHash =
                                    HashPassword(
                                        model.Password);

                                if (storedHash.Equals(
                                    enteredHash,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    HttpContext.Session.SetInt32(
                                        "MemberID",
                                        memberId);

                                    HttpContext.Session.SetString(
                                        "MemberName",
                                        fullName);

                                    TempData["SuccessMessage"] =
                                        "Login successful! Welcome, "
                                        + fullName + ".";

                                    return RedirectToAction(
                                        "MemberDashboard");
                                }
                            }
                        }
                    }
                }

                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Login failed: " + ex.Message);

                return View(model);
            }
        }


        // =====================================================
        // FORGOT PASSWORD - GET
        // =====================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        // =====================================================
        // FORGOT PASSWORD - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage =
                    "Please enter your registered email address.";

                return View();
            }

            try
            {
                using (var connection =
                       _database.GetConnection())
                {
                    connection.Open();

                    string sql = @"
                        SELECT MemberID
                        FROM MEMBER
                        WHERE Email = @Email";

                    using (var command =
                           new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@Email",
                            email.Trim());

                        object? result =
                            command.ExecuteScalar();

                        if (result == null)
                        {
                            ViewBag.ErrorMessage =
                                "No account was found with that email address.";

                            return View();
                        }
                    }
                }

                TempData["ResetEmail"] =
                    email.Trim();

                return RedirectToAction(
                    "ResetPassword");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Unable to process your request: "
                    + ex.Message;

                return View();
            }
        }


        // =====================================================
        // RESET PASSWORD - GET
        // =====================================================

        [HttpGet]
        public IActionResult ResetPassword()
        {
            string? email =
                TempData["ResetEmail"] as string;

            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            ViewBag.Email = email;

            TempData.Keep("ResetEmail");

            return View();
        }


        // =====================================================
        // RESET PASSWORD - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(
            string email,
            string newPassword,
            string confirmPassword)
        {
            ViewBag.Email = email;


            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.ErrorMessage =
                    "Please enter a new password.";

                return View();
            }


            if (newPassword.Length < 6)
            {
                ViewBag.ErrorMessage =
                    "Password must contain at least 6 characters.";

                return View();
            }


            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage =
                    "Passwords do not match.";

                return View();
            }


            try
            {
                string passwordHash =
                    HashPassword(newPassword);


                using (var connection =
                       _database.GetConnection())
                {
                    connection.Open();

                    string sql = @"
                        UPDATE MEMBER
                        SET PasswordHash = @PasswordHash
                        WHERE Email = @Email";


                    using (var command =
                           new SqlCommand(
                               sql,
                               connection))
                    {
                        command.Parameters.AddWithValue(
                            "@PasswordHash",
                            passwordHash);

                        command.Parameters.AddWithValue(
                            "@Email",
                            email.Trim());

                        int rows =
                            command.ExecuteNonQuery();


                        if (rows == 0)
                        {
                            ViewBag.ErrorMessage =
                                "Unable to update the password.";

                            return View();
                        }
                    }
                }


                TempData["SuccessMessage"] =
                    "Password reset successful. Please sign in with your new password.";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Password reset failed: "
                    + ex.Message;

                return View();
            }
        }


        // =====================================================
        // MEMBER DASHBOARD
        // =====================================================

        [HttpGet]
        public IActionResult MemberDashboard()
        {
            int? memberId =
                HttpContext.Session.GetInt32("MemberID");

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            ViewBag.MemberName =
                HttpContext.Session.GetString(
                    "MemberName");


            var upcomingBookingList =
                new List<Booking>();


            try
            {
                using var connection =
                    _database.GetConnection();

                connection.Open();


                string upcomingListSql = @"
                    SELECT TOP 5
                        B.BookingID,
                        B.MemberID,
                        B.FacilityID,
                        B.BookingDate,
                        B.StartTime,
                        B.EndTime,
                        B.BookingStatus,
                        F.FacilityName,
                        P.PaymentID,
                        P.Amount
                    FROM BOOKING B
                    INNER JOIN FACILITY F
                        ON B.FacilityID = F.FacilityID
                    LEFT JOIN PAYMENT P
                        ON B.PAYMENT_PaymentID =
                           P.PaymentID
                    WHERE B.MemberID = @MemberID
                      AND B.BookingDate >=
                          CAST(GETDATE() AS DATE)
                      AND B.BookingStatus IN
                          ('Confirmed', 'Pending')
                    ORDER BY
                        B.BookingDate ASC,
                        B.StartTime ASC";


                using (var command =
                       new SqlCommand(
                           upcomingListSql,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@MemberID",
                        memberId.Value);


                    using var reader =
                        command.ExecuteReader();


                    while (reader.Read())
                    {
                        upcomingBookingList.Add(
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

                                PaymentID =
                                    reader["PaymentID"] == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(
                                            reader["PaymentID"]),

                                Amount =
                                    reader["Amount"] == DBNull.Value
                                        ? 0m
                                        : Convert.ToDecimal(
                                            reader["Amount"])
                            });
                    }
                }


                // =================================================
                // UPCOMING BOOKING COUNT
                // =================================================

                string upcomingCountSql = @"
                    SELECT COUNT(*)
                    FROM BOOKING
                    WHERE MemberID = @MemberID
                      AND BookingDate >=
                          CAST(GETDATE() AS DATE)
                      AND BookingStatus IN
                          ('Confirmed', 'Pending')";


                using (var command =
                       new SqlCommand(
                           upcomingCountSql,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@MemberID",
                        memberId.Value);


                    ViewBag.UpcomingBookings =
                        Convert.ToInt32(
                            command.ExecuteScalar());
                }


                // =================================================
                // TOTAL BOOKINGS
                // =================================================

                string totalBookingsSql = @"
                    SELECT COUNT(*)
                    FROM BOOKING
                    WHERE MemberID = @MemberID";


                using (var command =
                       new SqlCommand(
                           totalBookingsSql,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@MemberID",
                        memberId.Value);


                    ViewBag.TotalBookings =
                        Convert.ToInt32(
                            command.ExecuteScalar());
                }


                // =================================================
                // TOTAL REVIEWS
                // =================================================

                string totalReviewsSql = @"
                    SELECT COUNT(*)
                    FROM REVIEW
                    WHERE MemberID = @MemberID";


                using (var command =
                       new SqlCommand(
                           totalReviewsSql,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@MemberID",
                        memberId.Value);


                    ViewBag.TotalReviews =
                        Convert.ToInt32(
                            command.ExecuteScalar());
                }


                // =================================================
                // TOTAL AMOUNT SPENT
                // =================================================

                string amountSpentSql = @"
                    SELECT ISNULL(SUM(P.Amount), 0)
                    FROM PAYMENT P
                    INNER JOIN BOOKING B
                        ON P.PaymentID =
                           B.PAYMENT_PaymentID
                    WHERE B.MemberID = @MemberID
                      AND P.PaymentStatus = 'Paid'";


                using (var command =
                       new SqlCommand(
                           amountSpentSql,
                           connection))
                {
                    command.Parameters.AddWithValue(
                        "@MemberID",
                        memberId.Value);


                    ViewBag.AmountSpent =
                        Convert.ToDecimal(
                            command.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                ViewBag.UpcomingBookings = 0;
                ViewBag.TotalBookings = 0;
                ViewBag.TotalReviews = 0;
                ViewBag.AmountSpent = 0m;

                ViewBag.ErrorMessage =
                    "Unable to load dashboard data: "
                    + ex.Message;
            }


            ViewBag.UpcomingBookingList =
                upcomingBookingList;


            return View();
        }


        // =====================================================
        // LOGOUT
        // =====================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Index",
                "Home");
        }


        // =====================================================
        // GET SPORTS
        // =====================================================

        private List<Sport> GetSports()
        {
            var sports =
                new List<Sport>();

            using (var connection =
                   _database.GetConnection())
            {
                connection.Open();

                string sql = @"
                    SELECT
                        SportID,
                        SportName
                    FROM SPORT
                    ORDER BY SportName";


                using (var command =
                       new SqlCommand(
                           sql,
                           connection))
                {
                    using (var reader =
                           command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sports.Add(
                                new Sport
                                {
                                    SportID =
                                        Convert.ToInt32(
                                            reader["SportID"]),

                                    SportName =
                                        reader["SportName"]
                                        .ToString() ?? ""
                                });
                        }
                    }
                }
            }

            return sports;
        }


        // =====================================================
        // PASSWORD HASHING
        // =====================================================

        private static string HashPassword(
            string password)
        {
            using (SHA256 sha256 =
                   SHA256.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        password);

                byte[] hash =
                    sha256.ComputeHash(bytes);

                return Convert.ToHexString(hash);
            }
        }
    }
}