namespace Pick_To_Ride.Models.Entities
{
    public class AuditLog
    {
        public Guid LogId { get; set; }
        public Guid ActorUserId { get; set; }
        public string ActionType { get; set; }
        public string TargetEntity { get; set; }
        public string TargetEntityId { get; set; } = Guid.NewGuid().ToString();
        public DateTime ActionTimestamp { get; set; }
        public string Description { get; set; }
    }
}
