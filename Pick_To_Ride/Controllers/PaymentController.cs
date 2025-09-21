
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace Pick_To_Ride.Controllers
{
    /// <summary>
    ///  - Require user to be authenticated before processing payment (redirects guests to Register)
    ///  - Verify booking ownership to prevent paying for other users' bookings
    ///  - Generate a booking code if it was not set
    ///  - Create Notification entries for customer and staff after successful payment
    ///  - Send confirmation email to customer (reads SMTP from configuration)
    ///  - Proper error handling and user-friendly TempData messages
    /// </summary>


    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public PaymentController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Helper: get current user id from claims
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var uid) ? uid : Guid.Empty;
        }

        // Helper: generate a short booking code (6 chars, easy to read)
        private string GenerateBookingCode()
        {
            var rng = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        // Helper: send email (reads SMTP from configuration)
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = _config["Email:From"];
                var password = _config["Email:Password"];

                if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password))
                {
                    // SMTP not configured — do not throw, just log (or set TempData for debugging)
                    Console.WriteLine("SMTP not configured (Email:From / Email:Password missing in configuration)");
                    return;
                }

                var fromAddress = new MailAddress(fromEmail, "Pick To Ride");
                var toAddress = new MailAddress(toEmail);

                using var smtp = new SmtpClient
                {
                    Host = _config["Email:Host"] ?? "smtp.gmail.com",
                    Port = int.TryParse(_config["Email:Port"], out var p) ? p : 587,
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
                // In a real app use proper logging. Here we write to console for visibility.
                Console.WriteLine($"Email error: {ex.Message}");
            }
        }

        // GET: /Payment/Index - shows list of payments or payment page depending on your app
        public async Task<IActionResult> Index()
        {
            var items = await _context.Payments.Include(p => p.Booking).OrderByDescending(p => p.PaymentDate).ToListAsync();
            return View(items);
        }

        /// <summary>
        /// Pay action: processes payment for a given bookingId.
        /// - Redirects unauthenticated users to Register (with returnUrl back to this action).
        /// - Ensures the current user owns the booking.
        /// - Saves payment record and updates booking status.
        /// - Creates notifications and sends email confirmation.
        /// </summary>
        public async Task<IActionResult> Pay(Guid bookingId)
        {
            // check booking exists
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound("Booking not found.");

            // ensure user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                var returnUrl = Url.Action("Pay", "Payment", new { bookingId });
                return RedirectToAction("Register", "Account", new { returnUrl });
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                TempData["Error"] = "User identity not found. Please login again.";
                return RedirectToAction("Error");
            }

            // verify user owns the booking
            if (booking.CustomerId != currentUserId)
            {
                // don't reveal booking details to other users
                return Forbid();
            }

            // Ensure booking code exists (some older bookings might not have code)
            if (string.IsNullOrWhiteSpace(booking.BookingCode))
            {
                booking.BookingCode = GenerateBookingCode();
            }

            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = booking.TotalAmount,
                Method = "Card",
                PaymentDate = DateTime.UtcNow,
                Status = PaymentStatus.Paid
            };

            try
            {
                // Save payment and update booking
                _context.Payments.Add(payment);
                booking.Status = BookingStatus.Booked;
                booking.UpdatedAt = DateTime.UtcNow;
                _context.Bookings.Update(booking);
                await _context.SaveChangesAsync();

                // Create notification for customer
                try
                {
                    var custNotification = new Notification
                    {
                        UserId = booking.CustomerId,
                        Title = $"Booking Confirmed - {booking.BookingCode}",
                        Message = $"Your booking ({booking.BookingCode}) is confirmed. Pickup: {booking.PickupLocation ?? "N/A"}. Total: {booking.TotalAmount:C}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(custNotification);

                    // Notify all staff users (if any)
                    var staffUsers = await _context.Staffs.Select(s => s.UserId).ToListAsync();
                    foreach (var uid in staffUsers)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = uid,
                            Title = "New Booking Received",
                            Message = $"New booking {booking.BookingCode} by user {booking.CustomerId}. Pick-up: {booking.PickupLocation}",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception nEx)
                {
                    // Notification creation failed — continue, but log
                    Console.WriteLine($"Notification error: {nEx.Message}");
                }

                // Send email to customer (best-effort)
                try
                {
                    var user = await _context.Users.FindAsync(booking.CustomerId);
                    if (user != null)
                    {
                        var toEmail = user.Email;
                        if (!string.IsNullOrWhiteSpace(toEmail))
                        {
                            var subject = $"Booking Confirmed - {booking.BookingCode}";
                            var body = $@"<p>Dear {user.FullName},</p>
<p>Your booking <strong>{booking.BookingCode}</strong> has been confirmed.</p>
<ul>
<li>Car: {booking.CarId}</li>
<li>Pickup: {booking.PickupLocation}</li>
<li>Period: {booking.StartDate.ToShortDateString()} - {booking.EndDate.ToShortDateString()}</li>
<li>Total: {booking.TotalAmount:C}</li>
</ul>
<p>Thank you for using Pick To Ride.</p>";
                            await SendEmailAsync(toEmail, subject, body);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Email sending error: {emailEx.Message}");
                }

                TempData["Success"] = "🎉 Payment successful! Your booking has been confirmed.";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                // In case of error, make a failed payment record to keep audit trail
                try
                {
                    var failed = new Payment
                    {
                        BookingId = bookingId,
                        Amount = 0,
                        Method = "Card",
                        PaymentDate = DateTime.UtcNow,
                        Status = PaymentStatus.Cancelled
                    };
                    _context.Payments.Add(failed);
                    await _context.SaveChangesAsync();
                }
                catch { /* swallow - avoid secondary exceptions */ }

                TempData["Error"] = "⚠️ Payment failed: " + ex.Message;
                return RedirectToAction("Error");
            }
        }

        // Success page
        public IActionResult Success() => View();

        // Cancelled page
        public IActionResult Cancelled() => View();

        // Error page
        public IActionResult Error() => View();
    }
}
