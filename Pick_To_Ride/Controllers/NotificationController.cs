using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Controllers;
using Pick_To_Ride.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

public class NotificationController : BaseController
{
    public NotificationController(ApplicationDbContext context) : base(context) { }

    // returns partial list for header (top 10 unread)
    public async Task<IActionResult> Panel()
    {
        // assume User.Identity.Name holds user identifier (or use claims)
        var userId = Guid.TryParse(User.Identity.Name, out var u) ? u : Guid.Empty;

        var list = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        return PartialView("_NotificationsPartial", list);
    }

    // Mark as read
    [HttpPost]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var n = await _context.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        n.IsRead = true;
        _context.Notifications.Update(n);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // Admin listing of all notifications
    public async Task<IActionResult> Index()
    {
        var all = await _context.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return View(all);
    }
}
