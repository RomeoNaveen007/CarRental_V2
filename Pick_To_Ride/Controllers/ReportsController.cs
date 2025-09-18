using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pick_To_Ride.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Reports";
            return View();
        }
    }
}
