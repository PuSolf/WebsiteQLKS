using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string SoPhong { get; set; } = null!;

    public int Tang { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? DuongDanHinhAnh { get; set; }

    public int RoomTypeId { get; set; }

    public int HotelId { get; set; }

    public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

    public virtual HotelInfo Hotel { get; set; } = null!;

    public virtual RoomType RoomType { get; set; } = null!;
}
