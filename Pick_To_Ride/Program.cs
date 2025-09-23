using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;

namespace Pick_To_Ride
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // ⚡ Register IHttpContextAccessor
            builder.Services.AddHttpContextAccessor();

            // Session setup
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                });

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("CarRental")));

            // ✅ Configure SMTP settings (comes from appsettings.json → "SmtpSettings")
            builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

            var app = builder.Build();

            // ✅ Seed default admin
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();

                var hasher = new PasswordHasher<User>();

                if (!context.Users.Any(u => u.UserName == "admin"))
                {
                    var admin = new User
                    {
                        UserId = Guid.NewGuid(),
                        FullName = "Default Admin",
                        UserName = "Admin",
                        Email = "raveenthirannaveen7@gmail.com",
                        PhoneNumber = "0773820477",
                        Role = "Admin",
                        IsActive = true,
                        Address = "System Address",
                        LicenceNumber = "A1234567",
                        NIC = "123456789V",
                        City = "System City",
                        CreatedDate = DateTime.UtcNow,
                        ProfileImage = "/uploads/profiles/obito.jpg",
                        PasswordHash = hasher.HashPassword(null, "Admin@123")
                    };

                    context.Users.Add(admin);
                    context.SaveChanges();
                }
            }

            // Configure middleware pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ✅ Session must be before Authentication/Authorization
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Guest}/{action=Index}/{id?}");

            app.Run();
        }

             // SmtpSettings POCO
            public class SmtpSettings
            {
                public string Host { get; set; }
                public int Port { get; set; } = 587;
                public string Username { get; set; }
                public string Password { get; set; }
                public bool UseSSL { get; set; } = true;
            }

    }
    
}
    

