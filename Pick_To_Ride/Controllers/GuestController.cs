using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models;
using System.Diagnostics;

namespace Car_Rental_System_01.Controllers
{
    public class GuestController : Controller
    {
        private readonly ILogger<GuestController> _logger;
        private readonly ApplicationDbContext _context; // ✅ Add this

        // Inject both ILogger and DbContext
        public GuestController(ILogger<GuestController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // ✅ Initialize
        }

        public IActionResult Index()
        {
            var cars = _context.Cars.ToList();  // EF Core context
            return View(cars);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult SearchAvailableCars(DateTime startDate, DateTime endDate)
        {
            // Get list of booked cars in the selected date range
            var bookedCarIds = _context.Bookings
                                .Where(b => b.StartDate <= endDate && b.EndDate >= startDate)
                                .Select(b => b.CarId)
                                .ToList();

            // Available cars
            var availableCars = _context.Cars
                                .Where(c => !bookedCarIds.Contains(c.CarId))
                                .ToList();

            ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.ToString("yyyy-MM-dd");

            return View("AvailableCars", availableCars);
        }
    }
}
