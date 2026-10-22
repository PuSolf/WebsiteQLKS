using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string HoTen { get; set; } = null!;

    public string? SoGiayTo { get; set; }

    public string? Email { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public int? UserId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual User? User { get; set; }
}
