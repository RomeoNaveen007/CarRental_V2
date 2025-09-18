using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid AuditLogId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Action is required")]
        [StringLength(100, ErrorMessage = "Action cannot exceed 100 characters")]
        public string Action { get; set; }

        [Required(ErrorMessage = "Entity Name is required")]
        [StringLength(100)]
        public string EntityName { get; set; }

        [Required(ErrorMessage = "Entity ID is required")]
        public string EntityId { get; set; }

        [Required(ErrorMessage = "PerformedBy is required")]
        [StringLength(100)]
        public string PerformedBy { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(500, ErrorMessage = "Details cannot exceed 500 characters")]
        public string Details { get; set; }
    }
}
