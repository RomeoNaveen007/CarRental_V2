using Microsoft.AspNetCore.Mvc;

namespace Pick_To_Ride.Controllers
{
    public class StaffDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
