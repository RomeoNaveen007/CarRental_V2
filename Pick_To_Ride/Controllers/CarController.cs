using Microsoft.AspNetCore.Mvc;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using Microsoft.EntityFrameworkCore;


namespace Pick_To_Ride.Controllers
{
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CarController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 🔹 Get All Cars
        public async Task<IActionResult> GetAllCars()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        // 🔹 Add Car - GET
        [HttpGet]
        public IActionResult AddCar()
        {
            return View();
        }

        // 🔹 Add Car - POST
        [HttpPost]
        public async Task<IActionResult> AddCar(CarViewModel carViewModel)
        {
            if (!ModelState.IsValid)
                return View(carViewModel);

            string wwwRootPath = _webHostEnvironment.WebRootPath;

            // Save Car Image
            string carImageFileName = null;
            if (carViewModel.ImageFile != null)
            {
                carImageFileName = Guid.NewGuid().ToString() + Path.GetExtension(carViewModel.ImageFile.FileName);
                string carImagePath = Path.Combine(wwwRootPath, "uploads/cars", carImageFileName);

                using (var stream = new FileStream(carImagePath, FileMode.Create))
                {
                    await carViewModel.ImageFile.CopyToAsync(stream);
                }
            }

            // Save Logo Image
            string logoFileName = null;
            if (carViewModel.ImageLogoFile != null)
            {
                logoFileName = Guid.NewGuid().ToString() + Path.GetExtension(carViewModel.ImageLogoFile.FileName);
                string logoPath = Path.Combine(wwwRootPath, "uploads/logos", logoFileName);

                using (var stream = new FileStream(logoPath, FileMode.Create))
                {
                    await carViewModel.ImageLogoFile.CopyToAsync(stream);
                }
            }

            // Map to Entity
            var car = new Car
            {
                CarId = Guid.NewGuid(),
                CarName = carViewModel.CarName,
                Brand = carViewModel.Brand,
                RegistrationNumber = carViewModel.RegistrationNumber,
                FuelType = carViewModel.FuelType,
                Transmission = carViewModel.Transmission,
                Seats = carViewModel.Seats,
                DailyRate = carViewModel.DailyRate,
                ImageURL = carImageFileName != null ? "/uploads/cars/" + carImageFileName : null,
                ImageLogo = logoFileName != null ? "/uploads/logos/" + logoFileName : null,
                Description = carViewModel.Description,
                Status = carViewModel.Status,
                Category = carViewModel.Category
            };

            await _context.Cars.AddAsync(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(GetAllCars));
        }

        // 🔹 Update Car - GET
        [HttpGet]
        public async Task<IActionResult> UpdateCar(Guid id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == id);
            if (car == null) return NotFound();

            var vm = new CarViewModel
            {
                CarId = car.CarId,
                CarName = car.CarName,
                Brand = car.Brand,
                RegistrationNumber = car.RegistrationNumber,
                FuelType = car.FuelType,
                Transmission = car.Transmission,
                Seats = car.Seats,
                DailyRate = car.DailyRate,
                ImageURL = car.ImageURL,
                ImageLogo = car.ImageLogo,
                Description = car.Description,
                Status = car.Status,
                Category = car.Category
            };

            return View(vm);
        }

        // 🔹 Update Car - POST
        [HttpPost]
        public async Task<IActionResult> UpdateCar(CarViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == vm.CarId);
            if (car == null) return NotFound();

            string wwwRootPath = _webHostEnvironment.WebRootPath;

            // Update Car Image if uploaded
            if (vm.ImageFile != null)
            {
                string carImageFileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ImageFile.FileName);
                string carImagePath = Path.Combine(wwwRootPath, "uploads/cars", carImageFileName);

                using (var stream = new FileStream(carImagePath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }

                car.ImageURL = "/uploads/cars/" + carImageFileName;
            }

            // Update Logo if uploaded
            if (vm.ImageLogoFile != null)
            {
                string logoFileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ImageLogoFile.FileName);
                string logoPath = Path.Combine(wwwRootPath, "uploads/logos", logoFileName);

                using (var stream = new FileStream(logoPath, FileMode.Create))
                {
                    await vm.ImageLogoFile.CopyToAsync(stream);
                }

                car.ImageLogo = "/uploads/logos/" + logoFileName;
            }

            // Update other fields
            car.CarName = vm.CarName;
            car.Brand = vm.Brand;
            car.RegistrationNumber = vm.RegistrationNumber;
            car.FuelType = vm.FuelType;
            car.Transmission = vm.Transmission;
            car.Seats = vm.Seats;
            car.DailyRate = vm.DailyRate;
            car.Description = vm.Description;
            car.Status = vm.Status;
            car.Category = vm.Category;

            _context.Cars.Update(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(GetAllCars));
        }

        // 🔹 Car Details
        public async Task<IActionResult> CarDetails(Guid id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == id);
            if (car == null) return NotFound();

            return View(car);
        }

        // 🔹 Delete Car - GET
        [HttpGet]
        public async Task<IActionResult> DeleteCar(Guid id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == id);
            if (car == null) return NotFound();

            return View(car);
        }

        // 🔹 Delete Car - POST
        [HttpPost, ActionName("DeleteCar")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == id);
            if (car == null) return NotFound();

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(GetAllCars));
        }
    }
}
