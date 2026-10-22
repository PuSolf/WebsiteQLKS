using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string HoTen { get; set; } = null!;

    public string ChucVu { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public bool IsActive { get; set; }

    public int UserId { get; set; }

    public int HotelId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual HotelInfo Hotel { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User User { get; set; } = null!;
}
