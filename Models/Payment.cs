using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public decimal SoTien { get; set; }

    public string PhuongThuc { get; set; } = null!;

    public DateTime ThoiGianThanhToan { get; set; }

    public string TrangThai { get; set; } = null!;

    public int BookingId { get; set; }

    public int? InvoiceId { get; set; }

    public int EmployeeId { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }
}
