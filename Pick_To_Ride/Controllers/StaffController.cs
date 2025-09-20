using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Pick_To_Ride.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<User> _passwordHasher;

        public StaffController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _passwordHasher = new PasswordHasher<User>();
        }

        // GET: /Staff
        public async Task<IActionResult> Index(string role = "Staff", string search = "")
        {
            ViewData["Role"] = role;
            ViewData["Title"] = role == "Staff" ? "General Staff" : "Drivers";

            var query = _context.Staffs.Include(s => s.User).AsQueryable();
            query = query.Where(s => s.User.Role == role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.User.FullName.Contains(search) ||
                    s.User.Email.Contains(search) ||
                    s.User.PhoneNumber.Contains(search));
            }

            var list = await query.OrderBy(s => s.User.FullName).ToListAsync();
            ViewData["Search"] = search;

            return View(list);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var staff = await _context.Staffs.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpGet]
        public IActionResult Create(string role = "Staff")
        {
            ViewBag.Role = role;
            ViewData["Title"] = role == "Staff" ? "Add Staff" : "Add Driver";
            return View(new StaffViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffViewModel vm, string role = "Staff")
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Role = role;
                ViewData["Title"] = role == "Staff" ? "Add Staff" : "Add Driver";
                return View(vm);
            }

            vm.Role = role;

            // Unique checks
            if (await _context.Users.AnyAsync(u => u.UserName == vm.UserName))
                ModelState.AddModelError(nameof(vm.UserName), "Username already exists.");

            if (await _context.Users.AnyAsync(u => u.NIC == vm.NIC))
                ModelState.AddModelError(nameof(vm.NIC), "NIC already exists.");

            if (!string.IsNullOrWhiteSpace(vm.LicenceNumber) &&
                await _context.Users.AnyAsync(u => u.LicenceNumber == vm.LicenceNumber))
                ModelState.AddModelError(nameof(vm.LicenceNumber), "Licence number already exists.");

            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError(nameof(vm.Password), "Password is required.");

            if (!ModelState.IsValid)
                return View(vm);

            // Handle profile image → temp folder
            if (vm.ProfileImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(vm.ProfileImageFile.FileName);
                string tempDir = Path.Combine(_env.WebRootPath, "uploads", "temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                var tempFullPath = Path.Combine(tempDir, fileName);
                using var stream = new FileStream(tempFullPath, FileMode.Create);
                await vm.ProfileImageFile.CopyToAsync(stream);

                vm.ProfileImagePath = "/uploads/temp/" + fileName;
                vm.ProfileImageFile = null;
            }

            // Store in session and generate OTP
            HttpContext.Session.SetString("PendingStaff", System.Text.Json.JsonSerializer.Serialize(vm));
            var otp = new Random().Next(1000, 9999).ToString();
            HttpContext.Session.SetString("StaffOtp", otp);

            await SendOtpEmail(vm.Email, otp);
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otpInput)
        {
            var storedOtp = HttpContext.Session.GetString("StaffOtp");
            var pendingStaffJson = HttpContext.Session.GetString("PendingStaff");

            if (storedOtp == null || pendingStaffJson == null)
            {
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("Create");
            }

            if (otpInput != storedOtp)
            {
                ModelState.AddModelError("", "Invalid OTP.");
                return View();
            }

            var vm = System.Text.Json.JsonSerializer.Deserialize<StaffViewModel>(pendingStaffJson);
            if (vm == null)
            {
                TempData["Error"] = "Unable to read registration data.";
                return RedirectToAction("Create");
            }

            // Uniqueness re-check (race conditions)
            if (await _context.Users.AnyAsync(u => u.NIC == vm.NIC))
            {
                ModelState.AddModelError(nameof(vm.NIC), "NIC already exists.");
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.LicenceNumber) &&
                await _context.Users.AnyAsync(u => u.LicenceNumber == vm.LicenceNumber))
            {
                ModelState.AddModelError(nameof(vm.LicenceNumber), "Licence number already exists.");
                return View(vm);
            }

            // Move profile image → final folder
            string finalProfileWebPath = null;
            if (!string.IsNullOrEmpty(vm.ProfileImagePath) && vm.ProfileImagePath.StartsWith("/uploads/temp/"))
            {
                var tempFileName = Path.GetFileName(vm.ProfileImagePath);
                var tempFullPath = Path.Combine(_env.WebRootPath, "uploads", "temp", tempFileName);
                if (System.IO.File.Exists(tempFullPath))
                {
                    var profilesDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(profilesDir)) Directory.CreateDirectory(profilesDir);
                    var finalFullPath = Path.Combine(profilesDir, tempFileName);

                    if (System.IO.File.Exists(finalFullPath))
                        System.IO.File.Delete(finalFullPath);

                    System.IO.File.Move(tempFullPath, finalFullPath);
                    finalProfileWebPath = "/uploads/profiles/" + tempFileName;
                }
            }

            // Save user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = $"{Capitalize(vm.FirstName)} {Capitalize(vm.LastName)}",
                UserName = vm.UserName,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                Role = vm.Role,
                IsActive = vm.IsActive,
                NIC = vm.NIC,
                LicenceNumber = vm.LicenceNumber,
                Address = vm.Address,
                City = vm.City,
                PasswordHash = _passwordHasher.HashPassword(null, vm.Password),
                ProfileImage = finalProfileWebPath,
                CreatedDate = DateTime.UtcNow
            };
            _context.Users.Add(user);

            var staff = new Staff
            {
                StaffId = Guid.NewGuid(),
                UserId = user.UserId,
                User = user,
                Availability = StaffAvailability.Available,
                Salary = vm.Salary
            };
            _context.Staffs.Add(staff);

            await _context.SaveChangesAsync();

            // Clear session
            HttpContext.Session.Remove("PendingStaff");
            HttpContext.Session.Remove("StaffOtp");

            return RedirectToAction("Index", new { role = vm.Role });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null) return NotFound();

            var names = (staff.User.FullName ?? "").Split(' ');
            string firstName = names.Length > 0 ? names[0] : "";
            string lastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            var vm = new StaffViewModel
            {
                StaffId = staff.StaffId,
                UserId = staff.UserId,
                FirstName = firstName,
                LastName = lastName,
                UserName = staff.User.UserName,
                Email = staff.User.Email,
                PhoneNumber = staff.User.PhoneNumber,
                NIC = staff.User.NIC,
                LicenceNumber = staff.User.LicenceNumber,
                Address = staff.User.Address,
                City = staff.User.City,
                ProfileImage = staff.User.ProfileImage,
                Role = staff.User.Role,
                IsActive = staff.User.IsActive,
                Salary = staff.Salary,
                Availability = staff.Availability // keep existing availability
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StaffViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm); // show errors
            }

            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == vm.StaffId);
            if (staff == null) return NotFound();

            // Update user info
            staff.User.FullName = $"{Capitalize(vm.FirstName)} {Capitalize(vm.LastName)}".Trim();
            staff.User.UserName = vm.UserName;
            staff.User.Email = vm.Email;
            staff.User.PhoneNumber = vm.PhoneNumber;
            staff.User.NIC = vm.NIC;
            staff.User.LicenceNumber = vm.LicenceNumber;
            staff.User.Address = vm.Address;
            staff.User.City = vm.City;
            staff.User.IsActive = vm.IsActive;

            // Update staff info
            staff.Salary = vm.Salary;
            // Don’t touch availability here (leave as is)

            // Profile Image
            if (vm.ProfileImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(vm.ProfileImageFile.FileName);
                string profilesDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(profilesDir)) Directory.CreateDirectory(profilesDir);
                string fullPath = Path.Combine(profilesDir, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await vm.ProfileImageFile.CopyToAsync(stream);

                // Delete old file
                if (!string.IsNullOrEmpty(staff.User.ProfileImage))
                {
                    string oldPath = Path.Combine(_env.WebRootPath, staff.User.ProfileImage.TrimStart('/').Replace("/", "\\"));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                staff.User.ProfileImage = "/uploads/profiles/" + fileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { role = staff.User.Role });
        }


        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null) return NotFound();

            return View(staff);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var staff = await _context.Staffs.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null) return NotFound();

            if (!string.IsNullOrEmpty(staff.User.ProfileImage))
            {
                string path = Path.Combine(_env.WebRootPath, staff.User.ProfileImage.TrimStart('/').Replace("/", "\\"));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Users.Remove(staff.User);
            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { role = staff.User.Role });
        }

        // 📧 Email sender helper
        private async Task SendOtpEmail(string toEmail, string otp)
        {
            var fromAddress = new MailAddress("ravinaveen016@gmail.com", "Pick to Ride");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "pqlvgvdmpvhdwudw"; // store in config

            string subject = "Your OTP Code";
            string body = $"Your OTP code is: {otp}";

            using (var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            })
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                await smtp.SendMailAsync(message);
            }
        }

        private string Capitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            input = input.Trim().ToLowerInvariant();
            return char.ToUpperInvariant(input[0]) + (input.Length > 1 ? input.Substring(1) : string.Empty);
        }
    }
}
