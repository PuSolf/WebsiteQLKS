using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class BookingDetail
{
    public int BookingDetailId { get; set; }

    public int SoDem { get; set; }

    public decimal DonGia { get; set; }

    public int BookingId { get; set; }

    public int RoomId { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
