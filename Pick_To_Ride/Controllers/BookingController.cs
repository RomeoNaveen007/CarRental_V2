using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Pick_To_Ride.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Car)
                    .Include(b => b.Customer)
                    .Include(b => b.Driver)
                    .ToListAsync();

                return View(bookings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading bookings: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Booking/Create
        public IActionResult Create(Guid carId)
        {
            var vm = new BookingViewModel
            {
                CarId = carId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1)
            };
            return View(vm);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var car = await _context.Cars.FindAsync(model.CarId);
                if (car == null) return NotFound();

                // Booking code = random 6 digit
                string bookingCode = new Random().Next(100000, 999999).ToString();

                decimal baseCharge = 150;
                decimal carCharge = car.DailyRate * (decimal)(model.EndDate - model.StartDate).TotalDays;
                decimal driverCharge = model.DriverRequired ? 2500 * (decimal)(model.EndDate - model.StartDate).TotalDays : 0;

                var booking = new Booking
                {
                    CarId = model.CarId,
                    CustomerId = (model.CustomerId),
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    PickupLocation = model.PickupLocation,
                    DriverRequired = model.DriverRequired,
                    BookingCode = bookingCode,
                    TotalAmount = baseCharge + carCharge + driverCharge,
                    Status = BookingStatus.Pending
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Booking created successfully!";
                return RedirectToAction("Details", new { id = booking.BookingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating booking: " + ex.Message);
                return View(model);
            }
        }

        // GET: Booking/Details
        public async Task<IActionResult> Details(Guid id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }
    }
    
}
