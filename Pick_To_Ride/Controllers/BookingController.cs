// Controllers/BookingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pick_To_Ride.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public BookingController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ✅ Helper: get logged-in user ID from claims
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty;
        }

        // ✅ Helper: generate unique 6-char booking code
        private string GenerateBookingCode()
        {
            var rng = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        // ✅ Helper: send email (reads SMTP from appsettings.json)
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = _config["Email:From"];
                var password = _config["Email:Password"];

                var fromAddress = new MailAddress(fromEmail, "Pick To Ride");
                var toAddress = new MailAddress(toEmail);

                using var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromAddress.Address, password)
                };

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                await smtp.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // log error in real app
                Console.WriteLine($"Email error: {ex.Message}");
            }
        }

        // ✅ Index: show user’s bookings
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Driver).ThenInclude(s => s.User)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ✅ Details
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();

            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .Include(b => b.Driver).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            if (booking.CustomerId != GetCurrentUserId()) return Forbid();

            return View(booking);
        }

        // ✅ Create GET
        public async Task<IActionResult> Create()
        {
            var model = new BookingViewModel
            {
                BookingCode = GenerateBookingCode(),
                AvailableCars = await _context.Cars.Where(c => c.IsActive).ToListAsync(),
                AvailableDrivers = await _context.Staffs.Include(s => s.User)
                    .Where(s => s.IsDriver && s.Availability == StaffAvailability.Available)
                    .ToListAsync()
            };
            return View(model);
        }

        // ✅ Create POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCars = await _context.Cars.Where(c => c.IsActive).ToListAsync();
                model.AvailableDrivers = await _context.Staffs.Include(s => s.User)
                    .Where(s => s.IsDriver && s.Availability == StaffAvailability.Available)
                    .ToListAsync();
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Forbid();

            model.CustomerId = userId;

            var booking = new Booking
            {
                BookingId = Guid.NewGuid(),
                CarId = model.CarId,
                CustomerId = model.CustomerId,
                DriverId = model.DriverRequired ? model.DriverId : null, // only assign driver if required
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                BookingCode = GenerateBookingCode(),
                Status = BookingStatus.Booked,
                TotalAmount = model.TotalAmount,
                PickupLocation = model.DriverRequired ? model.PickupLocation : null, // only ask location if driver required
                DriverRequired = model.DriverRequired,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var customer = await _context.Users.FindAsync(userId);
            if (customer != null)
            {
                string msg = $"Your booking {booking.BookingCode} is confirmed. Pickup: {booking.PickupLocation ?? "N/A"} on {booking.StartDate:yyyy-MM-dd}.";
                await SendEmailAsync(customer.Email, "Booking Confirmed", msg);

                _context.Notifications.Add(new Notification
                {
                    UserId = booking.CustomerId,
                    Title = "Booking Confirmed",
                    Message = msg,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Booking created successfully!";
            return RedirectToAction("Pay", "Payment");
        }

        // ✅ Edit GET
        public async Task<IActionResult> Edit(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.CustomerId != GetCurrentUserId()) return Forbid();

            var model = new BookingViewModel
            {
                BookingId = booking.BookingId,
                CarId = booking.CarId,
                DriverId = booking.DriverId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                BookingCode = booking.BookingCode,
                TotalAmount = booking.TotalAmount,
                PickupLocation = booking.PickupLocation,
                DriverRequired = booking.DriverRequired,
                AvailableCars = await _context.Cars.Where(c => c.IsActive).ToListAsync(),
                AvailableDrivers = await _context.Staffs.Include(s => s.User).ToListAsync()
            };

            return View(model);
        }

        // ✅ Edit POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BookingViewModel model)
        {
            if (id != model.BookingId) return BadRequest();

            if (!ModelState.IsValid)
            {
                model.AvailableCars = await _context.Cars.Where(c => c.IsActive).ToListAsync();
                model.AvailableDrivers = await _context.Staffs.Include(s => s.User).ToListAsync();
                return View(model);
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.CustomerId != GetCurrentUserId()) return Forbid();

            booking.CarId = model.CarId;
            booking.DriverId = model.DriverRequired ? model.DriverId : null;
            booking.StartDate = model.StartDate;
            booking.EndDate = model.EndDate;
            booking.TotalAmount = model.TotalAmount;
            booking.PickupLocation = model.DriverRequired ? model.PickupLocation : null;
            booking.DriverRequired = model.DriverRequired;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Booking/Cancel/{id} → show confirmation
        public async Task<IActionResult> Cancel(Guid id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Driver).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            if (booking.CustomerId != GetCurrentUserId()) return Forbid();

            return View(booking); // this will be a confirmation page
        }

        // POST: Booking/CancelConfirmed/{id} → actually cancel
        [HttpPost, ActionName("CancelConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.CustomerId != GetCurrentUserId()) return Forbid();

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully!";
            return RedirectToAction(nameof(Index));
        }

    }
}
