using Microsoft.AspNetCore.Mvc;
using CommunitySportsBooking.Models;

namespace CommunitySportsBooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseHelper _database;

        public HomeController(DatabaseHelper database)
        {
            _database = database;
        }

        public IActionResult Index()
        {
            try
            {
                using (var connection = _database.GetConnection())
                {
                    connection.Open();

                    ViewBag.DatabaseStatus =
                        "Database connection successful!";
                }
            }
            catch (Exception ex)
            {
                ViewBag.DatabaseStatus =
                    "Database connection failed: " + ex.Message;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}

