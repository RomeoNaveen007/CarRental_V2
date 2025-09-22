using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Catalog view - shows all cars with "Book Now"
        [AllowAnonymous]
        public async Task<IActionResult> Catalog()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        // GET: create booking page. carId optional (from Catalog->BookNow)
        [HttpGet]
        public async Task<IActionResult> Create(Guid? carId, string carName )
        {
            var vm = new BookingViewModel();
            vm.AvailableCars = await _context.Cars.ToListAsync();

            // drivers list for selection
            vm.AvailableDrivers = await _context.Staffs
                .Include(s => s.User)
                .Where(s => s.IsDriver)
                .ToListAsync();

            if (carId.HasValue)
            {
                vm.CarId = carId.Value;
                vm.SelectedCarName = carName ?? (await _context.Cars.FindAsync(carId.Value))?.CarName;
            }

            // if user is authenticated, set CustomerId
            if (User.Identity.IsAuthenticated)
            {
                var uid = Guid.Parse(User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                vm.CustomerId = uid;
            }

            // default dates
            vm.StartDate = DateTime.UtcNow.Date;
            vm.EndDate = DateTime.UtcNow.Date.AddDays(1);

            return View(vm);
        }

        // POST: create booking (saves Booking, redirects to Payment/Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel vm)
        {
            // repopulate dropdowns in case of error
            vm.AvailableCars = await _context.Cars.ToListAsync();
            vm.AvailableDrivers = await _context.Staffs.Include(s => s.User).Where(s => s.IsDriver).ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            // ensure customer exists (if not logged in, redirect to login/register while preserving model)
            if (!User.Identity.IsAuthenticated)
            {
                // store booking temp in TempData (serialize) or session - simple approach: put in TempData as JSON
                TempData["BookingTemp"] = System.Text.Json.JsonSerializer.Serialize(vm);
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ResumeBooking", "Booking") });
            }

            // ensure selected car exists
            var car = await _context.Cars.FindAsync(vm.CarId);
            if (car == null)
            {
                ModelState.AddModelError("", "Selected car not found.");
                return View(vm);
            }

            // calculate days inclusive: if same day assume 1 day
            var days = (vm.EndDate.Date - vm.StartDate.Date).Days;
            if (days < 0) { ModelState.AddModelError("EndDate", "End date must be after start date."); return View(vm); }
            days = Math.Max(1, days); // ensure at least 1
            // daily charges:
            var carDaily = car.DailyRate; // assume Car has DailyRate decimal
            var driverDaily = vm.DriverRequired ? 2000m : 0m;
            var bookingFee = 500m;
            var total = (carDaily + driverDaily) * days + bookingFee;

            var booking = new Booking
            {
                BookingId = Guid.NewGuid(),
                CarId = vm.CarId,
                CustomerId = vm.CustomerId,
                DriverId = vm.DriverId,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                BookingCode = GenerateBookingCode(),
                DriverRequired = vm.DriverRequired,
                PickupLocation = vm.DriverRequired ? vm.PickupLocation : null,
                TotalAmount = total,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // redirect to payment create for this booking
            return RedirectToAction("CreatePayment", "Payment", new { bookingId = booking.BookingId });
        }

        // helper: resume booking when guest logs in
        [Authorize]
        public IActionResult ResumeBooking()
        {
            if (TempData["BookingTemp"] == null)
            {
                return RedirectToAction("Catalog");
            }

            var json = TempData["BookingTemp"].ToString();
            var vm = System.Text.Json.JsonSerializer.Deserialize<BookingViewModel>(json);

            // set customer id from logged-in user
            var uid = Guid.Parse(User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            vm.CustomerId = uid;

            // show create view with prefilled vm
            vm.AvailableCars = _context.Cars.ToList();
            vm.AvailableDrivers = _context.Staffs.Include(s => s.User).Where(s => s.IsDriver).ToList();

            return View("Create", vm);
        }

        private string GenerateBookingCode()
        {
            // 6-char alphanumeric
            var chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var r = new Random();
            return new string(Enumerable.Range(0, 6).Select(i => chars[r.Next(chars.Length)]).ToArray());
        }

        // Index for Admin/Staff to list all bookings (CRUD)
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                                .Include(b => b.Car)
                                .Include(b => b.Customer)
                                .Include(b => b.Driver).ThenInclude(d => d.User)
                                .ToListAsync();
            return View(bookings);
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var vm = new BookingViewModel
            {
                BookingId = booking.BookingId,
                CarId = booking.CarId,
                CustomerId = booking.CustomerId,
                DriverId = booking.DriverId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                BookingCode = booking.BookingCode,
                DriverRequired = booking.DriverRequired,
                PickupLocation = booking.PickupLocation,
                TotalAmount = booking.TotalAmount,
         //       Status =  booking.Status,
                AvailableCars = _context.Cars.ToList(),
                AvailableDrivers = _context.Staffs.Include(s => s.User).ToList()
            };

            return View(vm);
        }

        [HttpPost, Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BookingViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var booking = await _context.Bookings.FindAsync(vm.BookingId);
            if (booking == null) return NotFound();

            booking.CarId = vm.CarId;
            booking.DriverId = vm.DriverId;
            booking.DriverRequired = vm.DriverRequired;
            booking.PickupLocation = vm.DriverRequired ? vm.PickupLocation : null;
            booking.StartDate = vm.StartDate;
            booking.EndDate = vm.EndDate;
            booking.TotalAmount = vm.TotalAmount;
           // booking.Status = vm.Status;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost, ActionName("Delete"), Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }
    }
}
