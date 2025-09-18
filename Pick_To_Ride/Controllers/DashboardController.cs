using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Controllers;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

public class DashboardController : BaseController
{
    public DashboardController(ApplicationDbContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        try
        {
            var totalBookings = await _context.Bookings.CountAsync();
            var totalRevenue = await _context.Payments.Where(p => p.Status == PaymentStatus.Paid).SumAsync(p => (decimal?)p.Amount) ?? 0;
            var activeCars = await _context.Cars.Where(c => c.Status == "Available").CountAsync();
            var driversOnDuty = await _context.Staffs.Where(s => s.Availability == StaffAvailability.OnDuty).CountAsync();

            // bookings per day last 7 days
            var last7 = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .Select(d => new {
                    Day = d.ToString("yyyy-MM-dd"),
                    Count = _context.Bookings.Count(b => b.CreatedAt.Date == d)
                }).Reverse().ToList();

            var model = new
            {
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                ActiveCars = activeCars,
                DriversOnDuty = driversOnDuty,
                BookingsPerDay = last7.Select(x => x.Count).ToArray(),
                Labels = last7.Select(x => x.Day).ToArray()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View();
        }
    }

        public IActionResult Privacy()
        {
            return View();
        }



        public IActionResult Pending()
        {
            return View();
        }
    
}
