namespace Pick_To_Ride.Models.Entities
{
    public class Payment
    {
        public Guid PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public Guid BookingId { get; set; }

    }
}
