using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class VwBookingSummary
{
    public int BookingId { get; set; }

    public string Customer { get; set; } = null!;

    public DateOnly NgayNhanPhong { get; set; }

    public DateOnly NgayTraPhong { get; set; }

    public string TrangThai { get; set; } = null!;

    public int? RoomCount { get; set; }

    public decimal TongTien { get; set; }

    public decimal TienCoc { get; set; }
}
