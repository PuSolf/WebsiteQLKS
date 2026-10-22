using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class RoomType
{
    public int RoomTypeId { get; set; }

    public string Ten { get; set; } = null!;

    public decimal Gia { get; set; }

    public int SoKhachToiDa { get; set; }

    public string? TienNghi { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
