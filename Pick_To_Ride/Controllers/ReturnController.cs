using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Controllers;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Threading.Tasks;

public class ReturnController : BaseController
{
    public ReturnController(ApplicationDbContext context) : base(context) { }

    [HttpGet]
    public IActionResult Create(Guid bookingId)
    {
        return View(new ReturnViewModel { BookingId = bookingId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReturnViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var returnRecord = new ReturnRecord
            {
                BookingId = model.BookingId,
                ReturnDate = DateTime.UtcNow,
                CarCondition = model.CarCondition,
                ExtraCharge = model.ExtraCharge
            };

            _context.ReturnRecords.Add(returnRecord);

            // Update booking & car status
            var booking = await _context.Bookings.FindAsync(model.BookingId);
            if (booking != null) booking.Status = BookingStatus.Completed;

            var car = await _context.Cars.FindAsync(booking.CarId);
            if (car != null) car.Status = "Available";

            await _context.SaveChangesAsync();

            await LogAuditAsync("Return", "Booking", model.BookingId.ToString(), User.Identity.Name, "Car returned");

            TempData["Success"] = "Car returned successfully.";
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
}
