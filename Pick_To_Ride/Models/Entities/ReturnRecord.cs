namespace Pick_To_Ride.Models.Entities
{
    public class ReturnRecord
    {
        public Guid ReturnId { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }

        //public Guid UserId { get; set; } // User who is returning the car and the driver and who receive it (get get via bookingId)

        public DateTime ReturnDate { get; set; }
        public string CarCondition { get; set; }
        public decimal ExtraCharge { get; set; } // e.g., for damages or late return


    }
}
