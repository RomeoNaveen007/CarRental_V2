namespace Pick_To_Ride.Models.Entities
{
    public class Maintenence
    {
        public Guid MaintenenceId { get; set; } = Guid.NewGuid();
        public Guid CarId { get; set; }
        public Guid ReportedBy { get; set; }
        public string MaintainenceType { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } // e.g. In Progress, Completed
        public decimal Cost { get; set; }

    }
}
