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
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payment/Index
        // Shows all payments (Admin/Staff use case)
        public async Task<IActionResult> Index()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Booking)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return View(payments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading payments: " + ex.Message;
                return View("Error");
            }
        }

        // GET: Payment/Details/{id}
        // Shows details of a single payment
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Invalid payment ID.");

            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
                return NotFound("Payment not found.");

            return View(payment);
        }

        // GET: Payment/Pay/{bookingId}
        // Creates a payment for a booking
        public async Task<IActionResult> Pay(Guid bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound("Booking not found.");

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
                _context.Payments.Add(payment);
                booking.Status = BookingStatus.Booked;
                await _context.SaveChangesAsync();

                TempData["Success"] = "🎉 Payment successful! Your booking has been confirmed.";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "⚠️ Payment failed: " + ex.Message;
                return RedirectToAction("Error");
            }
        }

        // GET: Payment/Cancel/{bookingId}
        // Cancels booking and records cancellation as a payment
        public async Task<IActionResult> Cancel(Guid bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return NotFound("Booking not found.");

            try
            {
                booking.Status = BookingStatus.Cancelled;

                var payment = new Payment
                {
                    BookingId = bookingId,
                    Amount = 0,
                    Method = "Card",
                    PaymentDate = DateTime.UtcNow,
                    Status = PaymentStatus.Cancelled
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                TempData["Info"] = "❌ Booking and payment cancelled.";
                return RedirectToAction("Cancelled");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Cancellation failed: " + ex.Message;
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
