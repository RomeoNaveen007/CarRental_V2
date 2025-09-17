namespace Pick_To_Ride.Models.Entities
{
    public class BookingExtentionRequest
    {
        public Guid Extention { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }
        public DateTime NewEndDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } // Pending, Approved, Rejected
        public DateTime RequestDate { get; set; }
        // property for who approved or rejected the request
        public Guid? ReviewedBy { get; set; } // Nullable to allow for pending requests


    }
}
