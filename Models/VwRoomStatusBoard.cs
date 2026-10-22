using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class VwRoomStatusBoard
{
    public string SoPhong { get; set; } = null!;

    public int Tang { get; set; }

    public string RoomType { get; set; } = null!;

    public decimal Gia { get; set; }

    public int SoKhachToiDa { get; set; }

    public string TrangThai { get; set; } = null!;
}
