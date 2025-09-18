using System;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class AuditLogViewModel
    {
        public Guid AuditLogId { get; set; }

        [Required(ErrorMessage = "Action is required")]
        [Display(Name = "Action Performed")]
        public string Action { get; set; }

        [Required(ErrorMessage = "Entity Name is required")]
        [Display(Name = "Entity Name")]
        public string EntityName { get; set; }

        [Required(ErrorMessage = "Entity ID is required")]
        [Display(Name = "Entity ID")]
        public string EntityId { get; set; }

        [Required(ErrorMessage = "User is required")]
        [Display(Name = "Performed By")]
        public string PerformedBy { get; set; }

        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string Details { get; set; }

    }
}
