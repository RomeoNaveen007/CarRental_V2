// Controllers/MaintenanceController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Controllers;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

public class MaintenanceController : BaseController
{
    public MaintenanceController(ApplicationDbContext context) : base(context) { }

    // List maintenance tasks
    public async Task<IActionResult> Index()
    {
        var list = await _context.Maintenences
            .Include(m => m) // placeholder
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MaintenanceViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(MaintenanceViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var maintenance = new Maintenence
            {
                CarId = model.CarId,
                ReportedBy = model.ReportedBy,
                MaintainenceType = model.MaintainenceType,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status,
                Cost = model.Cost
            };

            // mark car as unavailable during maintenance
            var car = await _context.Cars.FindAsync(model.CarId);
            if (car != null)
            {
                car.Status = "Maintenance";
                _context.Cars.Update(car);
            }

            _context.Maintenences.Add(maintenance);
            await _context.SaveChangesAsync();

            await LogAuditAsync("Create", "Maintenence", maintenance.MaintenenceId.ToString(), User.Identity.Name, "Created maintenance record");

            TempData["Success"] = "Maintenance record created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var m = await _context.Maintenences.FindAsync(id);
        if (m == null) return NotFound();

        var model = new MaintenanceViewModel
        {
            MaintenenceId = m.MaintenenceId,
            CarId = m.CarId,
            ReportedBy = m.ReportedBy,
            MaintainenceType = m.MaintainenceType,
            Description = m.Description,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            Status = m.Status,
            Cost = m.Cost
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(MaintenanceViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var m = await _context.Maintenences.FindAsync(model.MaintenenceId);
            if (m == null) return NotFound();

            m.MaintainenceType = model.MaintainenceType;
            m.Description = model.Description;
            m.StartDate = model.StartDate;
            m.EndDate = model.EndDate;
            m.Status = model.Status;
            m.Cost = model.Cost;

            _context.Maintenences.Update(m);

            // restore car status if maintenance completed
            if (m.Status == "Completed")
            {
                var car = await _context.Cars.FindAsync(m.CarId);
                if (car != null) car.Status = "Available";
            }

            await _context.SaveChangesAsync();
            await LogAuditAsync("Update", "Maintenence", m.MaintenenceId.ToString(), User.Identity.Name, "Updated maintenance record");

            TempData["Success"] = "Maintenance updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var m = await _context.Maintenences.FindAsync(id);
            if (m == null) return NotFound();

            _context.Maintenences.Remove(m);

            // restore car if required (simple logic: set available)
            var car = await _context.Cars.FindAsync(m.CarId);
            if (car != null) car.Status = "Available";

            await _context.SaveChangesAsync();
            await LogAuditAsync("Delete", "Maintenence", id.ToString(), User.Identity.Name, "Deleted maintenance record");

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
