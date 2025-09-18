using Microsoft.AspNetCore.Mvc.Rendering;
using Pick_To_Ride.Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class BookingViewModel
    {
        public Guid BookingId { get; set; }

        [Required]
        public Guid CarId { get; set; }
        public string CarName { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }

        public Guid? DriverId { get; set; }
        public string DriverName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public string PickupLocation { get; set; }

        public bool DriverRequired { get; set; }

        public decimal TotalAmount { get; set; }
        public string BookingCode { get; set; }
    }
}
