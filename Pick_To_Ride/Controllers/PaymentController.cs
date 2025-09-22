using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pick_To_Ride.Data;
using Pick_To_Ride.Helpers;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Pick_To_Ride.Program;

namespace Pick_To_Ride.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailHelper _emailHelper;

        public PaymentController(ApplicationDbContext context, IOptions<Program.SmtpSettings> smtpOptions)
        {
            _context = context;
            _emailHelper = new EmailHelper(smtpOptions);
        }

        // GET: show payment page for booking
        [HttpGet]
        public async Task<IActionResult> CreatePayment(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .Include(b => b.Driver).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            var vm = new PaymentViewModel
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalAmount,
                BookingDetails = $"Car: {booking.Car?.CarName} | From: {booking.StartDate:yyyy-MM-dd} To: {booking.EndDate:yyyy-MM-dd} | Code: {booking.BookingCode}",
                Status = "Pending"
            };

            return View(vm);
        }

        // POST: process payment (simulated)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(PaymentViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == vm.BookingId);

            if (booking == null) return NotFound();

            // create payment record
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                BookingId = booking.BookingId,
                Amount = vm.Amount,
                PaymentDate = DateTime.UtcNow,
                Method = vm.Method ?? "Card",
                Status = PaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);

            // update booking status to Booked
            booking.Status = BookingStatus.Booked;
            booking.UpdatedAt = DateTime.UtcNow;
            _context.Bookings.Update(booking);

            // If driver assigned and driver required, add DriverSchedule to block dates
            if (booking.DriverId.HasValue && booking.DriverRequired)
            {
                var schedule = new DriverSchedule
                {
                    StaffId = booking.DriverId.Value,
                    StartDate = booking.StartDate.Date,
                    EndDate = booking.EndDate.Date,
                    BookingId = booking.BookingId
                };
                _context.DriverSchedules.Add(schedule);

                // Optionally set Staff.Availability to OnDuty (single flag)
                var staff = await _context.Staffs.FindAsync(booking.DriverId.Value);
                if (staff != null) staff.Availability = StaffAvailability.OnDuty;
            }

            // create notification for user
            var notif = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = booking.CustomerId,
                Title = "Booking Confirmed",
                Message = $"Your booking {booking.BookingCode} is confirmed. Amount paid: {payment.Amount:C}.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notif);

            await _context.SaveChangesAsync();

            // send email about booking + payment
            var body = new StringBuilder();
            body.AppendLine($"<p>Hi {booking.Customer.FullName},</p>");
            body.AppendLine($"<p>Your booking <strong>{booking.BookingCode}</strong> has been confirmed.</p>");
            body.AppendLine($"<p>Car: {booking.Car?.CarName}<br/>From: {booking.StartDate:yyyy-MM-dd} To: {booking.EndDate:yyyy-MM-dd}<br/>Total: {booking.TotalAmount:C}</p>");
            body.AppendLine($"<p>Payment reference: {payment.PaymentId}</p>");
            body.AppendLine("<p>Thank you for choosing Pick To Ride.</p>");

            try
            {
                await _emailHelper.SendEmailAsync(booking.Customer.Email, "Booking Confirmed - PickToRide", body.ToString());
            }
            catch
            {
                // do not fail if email sending fails
            }

            TempData["Success"] = $"Booking {booking.BookingCode} confirmed and payment received.";
            // After success, redirect to a success page or my bookings depending on role
            return RedirectToAction("Success", new { bookingId = booking.BookingId });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Success(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();

            ViewBag.Message = $"Booking {booking.BookingCode} confirmed.";
            return View(booking);
        }
    }
}
