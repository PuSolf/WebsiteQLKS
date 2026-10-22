using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int SoDem { get; set; }

    public decimal GiamGia { get; set; }

    public decimal PhuThu { get; set; }

    public decimal TongTien { get; set; }

    public int BookingId { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
