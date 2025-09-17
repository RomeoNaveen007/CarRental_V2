using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        public Guid RecipientUserId { get; set; } // driver user id or admin user id

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
