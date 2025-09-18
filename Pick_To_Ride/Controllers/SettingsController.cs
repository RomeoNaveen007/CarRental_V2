using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pick_To_Ride.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "System Settings";
            return View();
        }
    }
}
