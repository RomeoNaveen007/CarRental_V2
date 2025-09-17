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
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<User> _passwordHasher;

        public UsersController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _passwordHasher = new PasswordHasher<User>();
        }

        // GET: /Users
        public async Task<IActionResult> Index(string search)
        {
            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.NIC.ToLower().Contains(search));
            }

            var users = await usersQuery.AsNoTracking().ToListAsync();
            return View(users);
        }

        // GET: /Users/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // GET: /Users/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var exists = await _context.Users.AnyAsync(u => u.UserName == vm.UserName);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.UserName), "Username already exists.");
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(nameof(vm.Password), "Password is required.");
                return View(vm);
            }

            // --- Handle uploaded profile image
            if (vm.ProfileImageFile != null)
            {
                var ext = Path.GetExtension(vm.ProfileImageFile.FileName) ?? "";
                var tempFileName = Guid.NewGuid().ToString() + ext;
                var tempDir = Path.Combine(_env.WebRootPath, "uploads", "temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                var tempFullPath = Path.Combine(tempDir, tempFileName);
                using (var stream = new FileStream(tempFullPath, FileMode.Create))
                {
                    await vm.ProfileImageFile.CopyToAsync(stream);
                }

                vm.ProfileImage = $"/uploads/temp/{tempFileName}";
                vm.ProfileImageFile = null;
            }

            // Generate OTP
            var otp = new Random().Next(1000, 9999).ToString();

            // Save pending VM and OTP in session
            HttpContext.Session.SetString("PendingUser", System.Text.Json.JsonSerializer.Serialize(vm));
            HttpContext.Session.SetString("UserOtp", otp);

            // Send OTP to email
            await SendOtpEmail(vm.Email, otp);

            return RedirectToAction("VerifyOtp");
        }

        // GET: /Users/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            return View();
        }

        // POST: /Users/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otpInput)
        {
            var storedOtp = HttpContext.Session.GetString("UserOtp");
            var pendingUserJson = HttpContext.Session.GetString("PendingUser");

            if (storedOtp == null || pendingUserJson == null)
            {
                ModelState.AddModelError("", "Session expired. Please register again.");
                return RedirectToAction("Create");
            }

            if (otpInput != storedOtp)
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                return View();
            }

            var vm = System.Text.Json.JsonSerializer.Deserialize<UserViewModel>(pendingUserJson);
            if (vm == null)
            {
                ModelState.AddModelError("", "Unable to read registration data. Please try again.");
                return RedirectToAction("Create");
            }

            // Uniqueness checks
            bool licenceExists = await _context.Users
                .AnyAsync(u => u.LicenceNumber == vm.LicenceNumber && u.UserId != vm.UserId);
            if (licenceExists)
            {
                ModelState.AddModelError(nameof(vm.LicenceNumber), "Licence number already exists.");
                return View(vm);
            }

            bool nicExists = await _context.Users
                .AnyAsync(u => u.NIC == vm.NIC && u.UserId != vm.UserId);
            if (nicExists)
            {
                ModelState.AddModelError(nameof(vm.NIC), "NIC already exists.");
                return View(vm);
            }

            // Move temp profile image → final folder
            string finalProfileWebPath = null;
            if (!string.IsNullOrEmpty(vm.ProfileImage) &&
                vm.ProfileImage.StartsWith("/uploads/temp/", StringComparison.OrdinalIgnoreCase))
            {
                var tempFileName = Path.GetFileName(vm.ProfileImage);
                var tempFullPath = Path.Combine(_env.WebRootPath, "uploads", "temp", tempFileName);

                if (System.IO.File.Exists(tempFullPath))
                {
                    var profilesDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(profilesDir)) Directory.CreateDirectory(profilesDir);

                    var finalFullPath = Path.Combine(profilesDir, tempFileName);

                    try
                    {
                        if (System.IO.File.Exists(finalFullPath))
                            System.IO.File.Delete(finalFullPath);

                        System.IO.File.Move(tempFullPath, finalFullPath);
                        finalProfileWebPath = $"/uploads/profiles/{tempFileName}";
                    }
                    catch
                    {
                        finalProfileWebPath = null;
                    }
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
                Address = vm.Address,
                City = vm.City,
                LicenceNumber = vm.LicenceNumber,
                NIC = vm.NIC,
                CreatedDate = DateTime.UtcNow,
                PasswordHash = _passwordHasher.HashPassword(null, vm.Password),
                ProfileImage = finalProfileWebPath
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Clear session
            HttpContext.Session.Remove("PendingUser");
            HttpContext.Session.Remove("UserOtp");

            return RedirectToAction("Index");
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

        // GET: /Users/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new UserViewModel
            {
                UserId = user.UserId,
                FirstName = SplitFirstName(user.FullName),
                LastName = SplitLastName(user.FullName),
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImage = user.ProfileImage,
                Role = user.Role,
                IsActive = user.IsActive,
                Address = user.Address,
                City = user.City,
                LicenceNumber = user.LicenceNumber,
                NIC = user.NIC
            };

            return View(vm);
        }

        // POST: /Users/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == vm.UserId);
            if (user == null) return NotFound();

            bool usernameExists = await _context.Users
                .AnyAsync(u => u.UserName == vm.UserName && u.UserId != vm.UserId);
            if (usernameExists)
            {
                ModelState.AddModelError(nameof(vm.UserName), "Username already exists.");
                return View(vm);
            }

            string wwwRootPath = _env.WebRootPath;

            // Update Profile Image
            if (vm.ProfileImageFile != null)
            {
                string profileFileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ProfileImageFile.FileName);
                string profilePath = Path.Combine(wwwRootPath, "uploads/profiles", profileFileName);

                if (!Directory.Exists(Path.GetDirectoryName(profilePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(profilePath));

                using (var stream = new FileStream(profilePath, FileMode.Create))
                {
                    await vm.ProfileImageFile.CopyToAsync(stream);
                }

                user.ProfileImage = "/uploads/profiles/" + profileFileName;
            }

            // Unique Licence/NIC checks
            bool licenceExists = await _context.Users
                .AnyAsync(u => u.LicenceNumber == vm.LicenceNumber && u.UserId != vm.UserId);
            if (licenceExists)
            {
                ModelState.AddModelError(nameof(vm.LicenceNumber), "Licence number already exists.");
                return View(vm);
            }

            bool nicExists = await _context.Users
                .AnyAsync(u => u.NIC == vm.NIC && u.UserId != vm.UserId);
            if (nicExists)
            {
                ModelState.AddModelError(nameof(vm.NIC), "NIC already exists.");
                return View(vm);
            }

            // Update fields
            user.FullName = $"{Capitalize(vm.FirstName)} {Capitalize(vm.LastName)}";
            user.UserName = vm.UserName;
            user.Email = vm.Email;
            user.PhoneNumber = vm.PhoneNumber;
            user.Role = vm.Role;
            user.IsActive = vm.IsActive;
            user.Address = vm.Address;
            user.City = vm.City;
            user.LicenceNumber = vm.LicenceNumber;
            user.NIC = vm.NIC;

            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, vm.Password);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: /Users/DeleteConfirmed/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(user.ProfileImage))
            {
                var file = Path.Combine(_env.WebRootPath,
                    user.ProfileImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                try
                {
                    if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
                }
                catch { }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        #region Helpers
        private string Capitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            input = input.Trim().ToLowerInvariant();
            return char.ToUpperInvariant(input[0]) +
                   (input.Length > 1 ? input.Substring(1) : string.Empty);
        }

        private string SplitFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : "";
        }

        private string SplitLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : "";
        }
        #endregion
    }
}