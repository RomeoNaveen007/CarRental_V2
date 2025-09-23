using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models;
using Pick_To_Ride.Models.Entities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Car_Rental_System_01.Controllers
{
    public class GuestController : Controller
    {
        private readonly ILogger<GuestController> _logger;
        private readonly ApplicationDbContext _context;

        public GuestController(ILogger<GuestController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Home action — returns all cars (kept for compatibility).
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        /// <summary>
        /// Catalog action — explicitly for the car catalog view.
        /// Use this action to render your catalog (Catalog.cshtml).
        /// </summary>
        public async Task<IActionResult> Catalog()
        {
            var cars = await _context.Cars.ToListAsync();
            return View("Catalog", cars);
        }

        /// <summary>
        /// Show details for a single car (View button target).
        /// URL: /Guest/Details/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || id == Guid.Empty)
                return BadRequest();

            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == id.Value);
            if (car == null)
                return NotFound();

            return View(car); // ensure Views/Guest/Details.cshtml exists
        }

     
        [HttpGet]
        public IActionResult BookPrompt(Guid carId)
        {
            var car = _context.Cars.FirstOrDefault(c => c.CarId == carId);
            if (car == null)
                return NotFound();

            // If user is logged in, redirect directly to Booking
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Create", "Booking", new { carId = car.CarId });
            }

            // If guest, always show modal popup partial
            return PartialView("_BookPrompt", car);
        }

        /// <summary>
        /// Search available cars between two dates (POST).
        /// Expects startDate and endDate (form-post). Returns Catalog view with filtered cars.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchAvailableCars(DateTime? startDate, DateTime? endDate)
        {
            // If dates aren't provided, show full catalog
            if (!startDate.HasValue || !endDate.HasValue)
            {
                TempData["Warning"] = "Please provide both start and end dates.";
                var all = await _context.Cars.ToListAsync();
                return View("Catalog", all);
            }

            // Normalize times (optional). Ensure start <= end
            var s = startDate.Value.Date;
            var e = endDate.Value.Date;
            if (s > e)
            {
                // swap if user reversed them
                var tmp = s;
                s = e;
                e = tmp;
            }

            // Find bookings that overlap requested window:
            // booking.StartDate <= e && booking.EndDate >= s
            var bookedCarIds = await _context.Bookings
                                        .Where(b => b.StartDate <= e && b.EndDate >= s)
                                        .Select(b => b.CarId)
                                        .Distinct()
                                        .ToListAsync();

            // Cars not in bookedCarIds are available
            var availableCars = await _context.Cars
                                        .Where(c => !bookedCarIds.Contains(c.CarId))
                                        .ToListAsync();

            ViewBag.StartDate = s.ToString("yyyy-MM-dd");
            ViewBag.EndDate = e.ToString("yyyy-MM-dd");

            return View("Catalog", availableCars);
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
    }
}
