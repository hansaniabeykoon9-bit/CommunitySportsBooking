using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using CommunitySportsBooking.Models;

namespace CommunitySportsBooking.Controllers
{
    public class InquiryController : Controller
    {
        private readonly DatabaseHelper _database;

        public InquiryController(DatabaseHelper database)
        {
            _database = database;
        }

        // =========================
        // SEND INQUIRY - GET
        // =========================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // =========================
        // SEND INQUIRY - POST
        // =========================

        [HttpPost]
        public IActionResult Create(
            string name,
            string email,
            string subject,
            string message)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["InquiryError"] =
                    "Please complete all fields.";

                return View();
            }

            try
            {
                using var connection = _database.GetConnection();
                connection.Open();

                int inquiryId;

                string idSql = @"
                    SELECT ISNULL(MAX(InquiryID), 0) + 1
                    FROM INQUIRY";

                using (var idCommand =
                       new SqlCommand(idSql, connection))
                {
                    inquiryId = Convert.ToInt32(
                        idCommand.ExecuteScalar());
                }


                string sql = @"
                    INSERT INTO INQUIRY
                    (
                        InquiryID,
                        Name,
                        Email,
                        Subject,
                        Message,
                        InquiryDate
                    )
                    VALUES
                    (
                        @InquiryID,
                        @Name,
                        @Email,
                        @Subject,
                        @Message,
                        GETDATE()
                    )";

                using var command =
                    new SqlCommand(sql, connection);

                command.Parameters.AddWithValue(
                    "@InquiryID",
                    inquiryId);

                command.Parameters.AddWithValue(
                    "@Name",
                    name.Trim());

                command.Parameters.AddWithValue(
                    "@Email",
                    email.Trim());

                command.Parameters.AddWithValue(
                    "@Subject",
                    subject.Trim());

                command.Parameters.AddWithValue(
                    "@Message",
                    message.Trim());

                command.ExecuteNonQuery();

                TempData["InquirySuccess"] =
                    "Your inquiry has been sent successfully. " +
                    "Our team will contact you soon.";

                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                TempData["InquiryError"] =
                    "Unable to send inquiry: " + ex.Message;

                return View();
            }
        }
    }
}
