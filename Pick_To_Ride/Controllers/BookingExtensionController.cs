using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pick_To_Ride.Controllers;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

public class BookingExtensionController : BaseController
{
    public BookingExtensionController(ApplicationDbContext context) : base(context) { }

    // Create new extension request
    [HttpGet]
    public IActionResult Create(Guid bookingId)
    {
        var model = new BookingExtensionViewModel { BookingId = bookingId };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingExtensionViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var booking = await _context.Bookings.FindAsync(model.BookingId);
            if (booking == null)
            {
                ModelState.AddModelError("", "Booking not found.");
                return View(model);
            }

            var car = await _context.Cars.FindAsync(booking.CarId);

            // Check if car is free for the new dates
            bool carAvailable = !_context.Bookings
                .Any(b => b.CarId == booking.CarId && b.BookingId != booking.BookingId &&
                          ((model.NewEndDate >= b.StartDate && model.NewEndDate <= b.EndDate) ||
                           (booking.StartDate >= b.StartDate && booking.StartDate <= b.EndDate)));

            bool driverAvailable = true;

            if (!string.IsNullOrEmpty(booking.DriverId))
            {
                var driverId = Guid.Parse(booking.DriverId);
                driverAvailable = !_context.Bookings
                    .Any(b => b.DriverId == driverId.ToString() && b.BookingId != booking.BookingId &&
                              ((model.NewEndDate >= b.StartDate && model.NewEndDate <= b.EndDate) ||
                               (booking.StartDate >= b.StartDate && booking.StartDate <= b.EndDate)));
            }

            var extension = new BookingExtentionRequest
            {
                BookingId = model.BookingId,
                NewEndDate = model.NewEndDate,
                Reason = model.Reason
            };

            if (carAvailable && driverAvailable)
            {
                extension.Status = "Approved";
                booking.EndDate = model.NewEndDate;
                await LogAuditAsync("BookingExtension", "Booking", booking.BookingId.ToString(), User.Identity.Name, "Booking automatically extended");

                // Notify customer
                var notify = new Notification
                {
                    UserId = Guid.Parse(booking.CustomerId),
                    Message = $"Your booking {booking.BookingCode} has been automatically extended to {booking.EndDate:d}."
                };
                _context.Notifications.Add(notify);
            }
            else
            {
                extension.Status = "Pending";
                await LogAuditAsync("BookingExtension", "Booking", booking.BookingId.ToString(), User.Identity.Name, "Booking extension pending admin approval");
            }

            _context.BookingExtentionRequests.Add(extension);
            await _context.SaveChangesAsync();

            TempData["Success"] = extension.Status == "Approved"
                ? "Booking extended successfully."
                : "Booking extension request submitted and pending approval.";

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    // Admin approve/reject pending requests
    [HttpGet]
    public async Task<IActionResult> Pending()
    {
        var pendingRequests = await _context.BookingExtentionRequests
            .Where(r => r.Status == "Pending")
            .Include(r => r.Booking)
            .ThenInclude(b => b.Car)
            .ToListAsync();

        return View(pendingRequests);
    }

    [HttpPost]
    public async Task<IActionResult> Review(Guid extensionId, bool approve)
    {
        var request = await _context.BookingExtentionRequests.FindAsync(extensionId);
        if (request == null) return NotFound();

        try
        {
            request.Status = approve ? "Approved" : "Rejected";
            request.ReviewedBy = Guid.Parse(User.Identity.Name); // assuming admin UserId
            if (approve)
            {
                var booking = await _context.Bookings.FindAsync(request.BookingId);
                booking.EndDate = request.NewEndDate;
                _context.Bookings.Update(booking);

                var notify = new Notification
                {
                    UserId = Guid.Parse(booking.CustomerId),
                    Message = $"Your booking {booking.BookingCode} has been approved for extension to {booking.EndDate:d}."
                };
                _context.Notifications.Add(notify);
            }

            await _context.SaveChangesAsync();

            await LogAuditAsync("BookingExtensionReview", "BookingExtension", request.ExtentionId.ToString(), User.Identity.Name, approve ? "Approved" : "Rejected");

            TempData["Success"] = approve ? "Request approved." : "Request rejected.";
            return RedirectToAction("Pending");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Pending");
        }
    }
}
