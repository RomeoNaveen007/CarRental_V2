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
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AuditLogs
        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new AuditLogViewModel
                {
                    AuditLogId = a.AuditLogId,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    PerformedBy = a.PerformedBy,
                    Timestamp = a.Timestamp,
                    Details = a.Details
                })
                .ToListAsync();

            return View(logs);
        }

        // GET: /AuditLogs/Details/{id}
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null) return NotFound();

            return View(new AuditLogViewModel
            {
                AuditLogId = auditLog.AuditLogId,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId,
                PerformedBy = auditLog.PerformedBy,
                Timestamp = auditLog.Timestamp,
                Details = auditLog.Details
            });
        }

        // GET: /AuditLogs/Create
        public IActionResult Create() => View();

        // POST: /AuditLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AuditLogViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var auditLog = new AuditLog
                    {
                        AuditLogId = Guid.NewGuid(),
                        Action = vm.Action,
                        EntityName = vm.EntityName,
                        EntityId = vm.EntityId,
                        PerformedBy = vm.PerformedBy,
                        Timestamp = DateTime.UtcNow,
                        Details = vm.Details
                    };

                    _context.Add(auditLog);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving AuditLog: {ex.Message}");
                }
            }
            return View(vm);
        }

        // GET: /AuditLogs/Edit/{id}
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null) return NotFound();

            return View(new AuditLogViewModel
            {
                AuditLogId = auditLog.AuditLogId,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId,
                PerformedBy = auditLog.PerformedBy,
                Timestamp = auditLog.Timestamp,
                Details = auditLog.Details
            });
        }

        // POST: /AuditLogs/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AuditLogViewModel vm)
        {
            if (id != vm.AuditLogId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var auditLog = await _context.AuditLogs.FindAsync(id);
                    if (auditLog == null) return NotFound();

                    auditLog.Action = vm.Action;
                    auditLog.EntityName = vm.EntityName;
                    auditLog.EntityId = vm.EntityId;
                    auditLog.PerformedBy = vm.PerformedBy;
                    auditLog.Details = vm.Details;

                    _context.Update(auditLog);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", $"Database error: {ex.Message}");
                }
            }
            return View(vm);
        }

        // GET: /AuditLogs/Delete/{id}
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null) return NotFound();

            return View(new AuditLogViewModel
            {
                AuditLogId = auditLog.AuditLogId,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId,
                PerformedBy = auditLog.PerformedBy,
                Timestamp = auditLog.Timestamp,
                Details = auditLog.Details
            });
        }

        // POST: /AuditLogs/Delete/{id}
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null) return NotFound();

            try
            {
                _context.AuditLogs.Remove(auditLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error deleting AuditLog: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
