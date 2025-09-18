using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;
using static Pick_To_Ride.Program;

namespace Pick_To_Ride.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create an audit log entry and save.
        /// performedBy should be a user identifier or name.
        /// </summary>
        protected async Task LogAuditAsync(string action, string entityName, string entityId, string performedBy, string details = "")
        {
            try
            {
                var log = new AuditLog
                {
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    PerformedBy = performedBy,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // never throw from audit logging - fail silently
            }
        }

        /// <summary>
        /// Send email using SMTP settings from configuration (synchronous SmtpClient usage for simplicity).
        /// Use try/catch in callers.
        /// </summary>
        protected void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpSection = HttpContext.RequestServices.GetService(typeof(IOptions<SmtpSettings>)) as IOptions<SmtpSettings>;
                var smtp = smtpSection?.Value;
                using var client = new System.Net.Mail.SmtpClient(smtp.Host, smtp.Port)
                {
                    EnableSsl = smtp.UseSSL,
                    Credentials = new System.Net.NetworkCredential(smtp.Username, smtp.Password)
                };
                var mail = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(smtp.Username, "Pick To Ride"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mail.To.Add(toEmail);
                client.Send(mail);
            }
            catch
            {
                // ignore mail errors (optionally log with audit)
            }
        }
    }
}
