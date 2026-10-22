using System.ComponentModel.DataAnnotations;

namespace WebsiteQLKS.ViewModels;

public sealed class CreateBookingRequest
{
    [Range(1, int.MaxValue)] public int RoomId { get; set; }
    // Optional and only honored for Manager/Receptionist requests; CustomerId is derived from the signed-in user for customers.
    [Range(1, int.MaxValue)] public int? CustomerId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    [Range(1, 20)] public int Guests { get; set; } = 1;
    [StringLength(300)] public string? Note { get; set; }
}

public sealed class UpdateBookingRequest
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    [Range(1, int.MaxValue)] public int RoomId { get; set; }
}

public sealed class CreatePaymentRequest
{
    [Range(1, int.MaxValue)] public int BookingId { get; set; }
    [Range(typeof(decimal), "0.01", "999999999999")] public decimal SoTien { get; set; }
    [Required, RegularExpression("^(Cash|BankTransfer|Card)$")] public string PhuongThuc { get; set; } = "Cash";
    public int? InvoiceId { get; set; }
}

public sealed class CheckoutRequest
{
    [Range(typeof(decimal), "0", "999999999999")] public decimal GiamGia { get; set; }
    [Range(typeof(decimal), "0", "999999999999")] public decimal PhuThu { get; set; }
}

public sealed class CreateRoomRequest
{
    [Required, StringLength(10)] public string SoPhong { get; set; } = "";
    [Range(0, 100)] public int Tang { get; set; }
    [Required, RegularExpression("^(Available|Reserved|Occupied|Maintenance|NeedsCleaning)$")] public string TrangThai { get; set; } = "Available";
    [StringLength(260)] public string? DuongDanHinhAnh { get; set; }
    [Range(1, int.MaxValue)] public int RoomTypeId { get; set; }
    [Range(1, int.MaxValue)] public int HotelId { get; set; } = 1;
}

public sealed class CreateRoomTypeRequest
{
    [Required, StringLength(50)] public string Ten { get; set; } = "";
    [Range(typeof(decimal), "0", "999999999999")] public decimal Gia { get; set; }
    [Range(1, 20)] public int SoKhachToiDa { get; set; }
    [StringLength(500)] public string? TienNghi { get; set; }
}

public sealed class CreateEmployeeRequest
{
    [Required, StringLength(100)] public string HoTen { get; set; } = "";
    [Required, RegularExpression("^(Receptionist)$")] public string ChucVu { get; set; } = "Receptionist";
    [Phone, StringLength(20)] public string? SoDienThoai { get; set; }
    [Required, StringLength(50, MinimumLength = 4)] public string Username { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
    [Range(1, int.MaxValue)] public int HotelId { get; set; } = 1;
}

public sealed class UpdateHotelInfoRequest
{
    [Required, StringLength(150)] public string Ten { get; set; } = "";
    [Required, StringLength(250)] public string DiaChi { get; set; } = "";
    [Required, Phone, StringLength(20)] public string SoDienThoai { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    public TimeOnly GioNhanPhong { get; set; } = new(14, 0);
    public TimeOnly GioTraPhong { get; set; } = new(12, 0);
}
