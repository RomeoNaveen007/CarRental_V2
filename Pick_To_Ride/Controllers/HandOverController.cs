using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Controllers;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

public class HandOverController : BaseController
{
    public HandOverController(ApplicationDbContext context) : base(context) { }

    // Show handover form
    [HttpGet]
    public IActionResult Create(Guid bookingId)
    {
        var model = new HandOverViewModel { BookingId = bookingId };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HandOverViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Validate Booking
            var booking = await _context.Bookings.FindAsync(model.BookingId);
            if (booking == null)
            {
                ModelState.AddModelError(string.Empty, "Booking not found.");
                return View(model);
            }

            // Compare Booking Codes
            if (!string.Equals(booking.BookingCode, model.BookingCode, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "Booking code mismatch.");
                return View(model);
            }

            // Parse CustomerId safely
            if (!Guid.TryParse(booking.CustomerId, out Guid customerId))
            { 
                ModelState.AddModelError(string.Empty, "Invalid customer ID in booking.");
                return View(model);
            }

            // Create HandOver record
            var handover = new HandOverRecord 
            { BookingId = model.BookingId, 
                UserId = Guid.Parse(booking.CustomerId), 
                HandOverDate = DateTime.UtcNow 
            };

            _context.HandOverRecords.Add(handover);

            // Update Car status
            var car = await _context.Cars.FindAsync(booking.CarId);
            if (car != null)
            {
                car.Status = "OnDuty";
            }

            await _context.SaveChangesAsync();

            // Audit log
            await LogAuditAsync("Handover", "Booking", booking.BookingId.ToString(), User.Identity?.Name ?? "System", "Car handed over");

            TempData["Success"] = "Handover successful.";
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            // Optionally log ex to a logger here
            ModelState.AddModelError(string.Empty, "An error occurred: " + ex.Message);
            return View(model);
        }
    }

}
