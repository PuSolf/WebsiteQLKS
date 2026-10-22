using System.ComponentModel.DataAnnotations;

namespace WebsiteQLKS.ViewModels;

public sealed class RoomSearchRequest
{
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int? RoomTypeId { get; set; }
    [Range(1, 20)] public int? Guests { get; set; }
    [Range(0, 100000000)] public decimal? MinPrice { get; set; }
    [Range(0, 100000000)] public decimal? MaxPrice { get; set; }
    [StringLength(60)] public string? Keyword { get; set; }
    [Range(1, 10000)] public int Page { get; set; } = 1;
    [Range(1, 48)] public int PageSize { get; set; } = 8;
}

public sealed class RoomCardDto
{
    public int RoomId { get; set; }
    public string SoPhong { get; set; } = "";
    public int Tang { get; set; }
    public string TrangThai { get; set; } = "Available";
    public string? DuongDanHinhAnh { get; set; }
    public int RoomTypeId { get; set; }
    public string LoaiPhong { get; set; } = "";
    public decimal Gia { get; set; }
    public int SoKhachToiDa { get; set; }
    public string? TienNghi { get; set; }
    public bool ConTrong { get; set; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize));
}
