using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required, StringLength(250)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
