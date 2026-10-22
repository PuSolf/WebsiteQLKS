using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public DateOnly NgayNhanPhong { get; set; }

    public DateOnly NgayTraPhong { get; set; }

    public string TrangThai { get; set; } = null!;

    public decimal TienCoc { get; set; }

    public decimal TongTien { get; set; }

    public DateTime? ThoiGianNhanPhongThucTe { get; set; }

    public DateTime? ThoiGianTraPhongThucTe { get; set; }

    public DateTime? ThoiGianHuy { get; set; }

    public int CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
