using System;
using System.Collections.Generic;

namespace WebsiteQLKS.Models;

public partial class HotelInfo
{
    public int HotelId { get; set; }

    public string Ten { get; set; } = null!;

    public string DiaChi { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string? Email { get; set; }

    public TimeOnly GioNhanPhong { get; set; }

    public TimeOnly GioTraPhong { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
