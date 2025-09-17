using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public Guid CarId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime BookingDate {  get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public Decimal TotalAmount { get; set; }
    }
}
