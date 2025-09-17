namespace Pick_To_Ride.Models.Entities
{
    public class HandOverRecord
    {
        public Guid HandOverId { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; } // User who is handing over the car and the driver and who receive it 
        public DateTime HandOverDate { get; set; }

    }
}
