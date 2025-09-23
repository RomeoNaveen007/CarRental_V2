using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using Pick_To_Ride.ViewModels;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Pick_To_Ride.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ===================== REGISTER =====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            if (await _context.Users.AnyAsync(u => u.UserName == vm.UserName))
            {
                ModelState.AddModelError(nameof(vm.UserName), "Username already exists.");
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(nameof(vm.Password), "Password is required.");
                return View(vm);
            }

            // Handle profile image
            if (vm.ProfileImageFile != null)
            {
                var ext = Path.GetExtension(vm.ProfileImageFile.FileName) ?? "";
                var tempFileName = Guid.NewGuid() + ext;
                var tempDir = Path.Combine(_env.WebRootPath, "uploads", "temp");
                Directory.CreateDirectory(tempDir);

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

            HttpContext.Session.SetString("PendingUser", JsonSerializer.Serialize(vm));
            HttpContext.Session.SetString("UserOtp", otp);

            await SendOtpEmail(vm.Email, otp);

            return RedirectToAction("VerifyOtp");
        }

        // ===================== OTP VERIFICATION =====================
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otpInput)
        {
            var storedOtp = HttpContext.Session.GetString("UserOtp");
            var pendingUserJson = HttpContext.Session.GetString("PendingUser");

            if (storedOtp == null || pendingUserJson == null)
            {
                ModelState.AddModelError("", "Session expired. Please register again.");
                return RedirectToAction("Register");
            }

            if (otpInput != storedOtp)
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                return View();
            }

            var vm = JsonSerializer.Deserialize<RegisterViewModel>(pendingUserJson);
            if (vm == null)
            {
                ModelState.AddModelError("", "Unable to read registration data. Please try again.");
                return RedirectToAction("Register");
            }

            // Uniqueness checks
            if (await _context.Users.AnyAsync(u => u.NIC == vm.NIC))
            {
                ModelState.AddModelError(nameof(vm.NIC), "NIC already exists.");
                return View(vm);
            }

            if (await _context.Users.AnyAsync(u => u.LicenceNumber == vm.LicenceNumber))
            {
                ModelState.AddModelError(nameof(vm.LicenceNumber), "Licence number already exists.");
                return View(vm);
            }

            // Move profile image from temp to permanent folder
            string finalProfileWebPath = null;
            if (!string.IsNullOrEmpty(vm.ProfileImage) &&
                vm.ProfileImage.StartsWith("/uploads/temp/", StringComparison.OrdinalIgnoreCase))
            {
                var tempFileName = Path.GetFileName(vm.ProfileImage);
                var tempFullPath = Path.Combine(_env.WebRootPath, "uploads", "temp", tempFileName);

                if (System.IO.File.Exists(tempFullPath))
                {
                    var profilesDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    Directory.CreateDirectory(profilesDir);

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

            // Create and save user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = $"{Capitalize(vm.FirstName)} {Capitalize(vm.LastName)}",
                UserName = vm.UserName,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                Role = "Customer",
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

            HttpContext.Session.Remove("PendingUser");
            HttpContext.Session.Remove("UserOtp");

            return RedirectToAction("Index", "Customer");
        }

        // ===================== LOGIN =====================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == vm.UserName && u.IsActive);
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(vm);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FullName", user.FullName ?? user.UserName),
                new Claim("ProfileImage", user.ProfileImage ?? "/uploads/profiles/default.png"),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // ===================== EMAIL SENDER =====================
        private async Task SendOtpEmail(string toEmail, string otp)
        {
            var fromAddress = new MailAddress("ravinaveen016@gmail.com", "Pick to Ride");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "pqlvgvdmpvhdwudw"; // ❗ Move to appsettings.json or secret manager

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

        // ===================== HELPER =====================
        private string Capitalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }
}
