using Pick_To_Ride.Models.Entities;

public static class AvailabilityHelper
{
    // return true if requested (start,end) overlaps existing booking
    public static bool IsCarAvailable(IEnumerable<Booking> bookings, DateTime start, DateTime end)
    {
        foreach (var b in bookings)
        {
            if (b.Status == BookingStatus.Cancelled) continue;
            if (start <= b.EndDate && end >= b.StartDate) return false;
        }
        return true;
    }
}
