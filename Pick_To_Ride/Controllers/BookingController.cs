using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
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

        // ✅ Catalog view - shows all cars with "Book Now"
        [AllowAnonymous]
        public async Task<IActionResult> Catalog()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        // ✅ Show booking form
        [HttpGet]
        public async Task<IActionResult> Create(Guid? carId, string carName)
        {
            var vm = new BookingViewModel
            {
                AvailableCars = await _context.Cars.ToListAsync(),
                AvailableDrivers = await _context.Staffs
                    .Include(s => s.User)
                    .Where(s => s.IsDriver)
                    .ToListAsync(),
                StartDate = DateTime.Now.Date,
                EndDate = DateTime.Now.Date.AddDays(1)
            };

            if (carId.HasValue)
            {
                vm.CarId = carId.Value;
                vm.SelectedCarName = !string.IsNullOrEmpty(carName)
                    ? carName
                    : (await _context.Cars.FindAsync(carId.Value))?.CarName;
            }

            if (User.Identity.IsAuthenticated)
            {
                var uidString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(uidString, out var uid))
                {
                    vm.CustomerId = uid;
                }
            }

            return View(vm);
        }

        // ✅ Handle booking submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel vm)
        {
            vm.AvailableCars = await _context.Cars.ToListAsync();
            vm.AvailableDrivers = await _context.Staffs.Include(s => s.User).Where(s => s.IsDriver).ToListAsync();

            if (!User.Identity.IsAuthenticated)
            {
                TempData["BookingTemp"] = System.Text.Json.JsonSerializer.Serialize(vm);
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ResumeBooking", "Booking") });
            }

            var car = await _context.Cars.FindAsync(vm.CarId);
            if (car == null)
            {
                ModelState.AddModelError("", "Selected car not found.");
                return View(vm);
            }

            var uid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            vm.CustomerId = uid;
            vm.SelectedCarName = car.CarName;

            var days = (vm.EndDate.Date - vm.StartDate.Date).Days;
            if (days < 0)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
                return View(vm);
            }
            days = Math.Max(1, days);

            if (!ModelState.IsValid) return View(vm);

            // ✅ Calculate totals
            var carDaily = car.DailyRate;
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

            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving booking: {ex.Message}");
                return View(vm);
            }

            return RedirectToAction("CreatePayment", "Payment", new { bookingId = booking.BookingId });
        }

        // Helper: Resume booking when guest logs in
        [Authorize]
        public IActionResult ResumeBooking()
        {
            if (TempData["BookingTemp"] == null)
            {
                return RedirectToAction("Catalog");
            }

            var json = TempData["BookingTemp"].ToString();
            var vm = System.Text.Json.JsonSerializer.Deserialize<BookingViewModel>(json);

            var uid = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            vm.CustomerId = uid;

            vm.AvailableCars = _context.Cars.ToList();
            vm.AvailableDrivers = _context.Staffs.Include(s => s.User).Where(s => s.IsDriver).ToList();

            return View("Create", vm);
        }

        private string GenerateBookingCode()
        {
            var chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var r = new Random();
            return new string(Enumerable.Range(0, 6).Select(i => chars[r.Next(chars.Length)]).ToArray());
        }

        // ✅ Admin/Staff Index
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

        // ✅ Edit (Admin/Staff only)
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
            booking.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating booking: {ex.Message}");
                return View(vm);
            }

            return RedirectToAction("Index");
        }

        // ✅ Delete
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
