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
    public async Task<IActionResult> Create(HandOverViewModel model)
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

            if (booking.BookingCode != model.BookingCode)
            {
                ModelState.AddModelError("", "Booking code mismatch.");
                return View(model);
            }

            var handover = new HandOverRecord
            {
                BookingId = model.BookingId,
                UserId = Guid.Parse(booking.CustomerId),
                HandOverDate = DateTime.UtcNow
            };

            _context.HandOverRecords.Add(handover);

            // Update car availability to OnDuty if driver is assigned
            var car = await _context.Cars.FindAsync(booking.CarId);
            if (car != null) car.Status = "OnDuty";

            await _context.SaveChangesAsync();

            await LogAuditAsync("Handover", "Booking", model.BookingId.ToString(), User.Identity.Name, "Car handed over");

            TempData["Success"] = "Handover successful.";
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
}
