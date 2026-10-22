using Microsoft.EntityFrameworkCore;
using WebsiteQLKS.Models;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Services;

public interface IHotelService
{
    Task<PagedResult<RoomCardDto>> SearchRoomsAsync(RoomSearchRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateBookingAsync(int userId, int? employeeId, CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task CancelBookingAsync(int bookingId, int? customerId, CancellationToken cancellationToken = default);
}

public sealed class HotelService(HotelManagementDbContext db, ILogger<HotelService> logger) : IHotelService
{
    public async Task<PagedResult<RoomCardDto>> SearchRoomsAsync(RoomSearchRequest request, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var from = request.CheckIn ?? today;
        var to = request.CheckOut ?? from.AddDays(1);
        if (from < today) throw new ArgumentException("Ngày nhận phòng không được ở quá khứ.");
        if (to <= from) throw new ArgumentException("Ngày trả phòng phải sau ngày nhận phòng.");
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
            throw new ArgumentException("Giá tối thiểu không được lớn hơn giá tối đa.");

        var query = db.Rooms.AsNoTracking()
            .Include(r => r.RoomType)
            .Where(r => r.TrangThai != "Maintenance" && r.TrangThai != "NeedsCleaning")
            .Where(r => !request.RoomTypeId.HasValue || r.RoomTypeId == request.RoomTypeId.Value)
            .Where(r => !request.Guests.HasValue || r.RoomType.SoKhachToiDa >= request.Guests.Value)
            .Where(r => !request.MinPrice.HasValue || r.RoomType.Gia >= request.MinPrice.Value)
            .Where(r => !request.MaxPrice.HasValue || r.RoomType.Gia <= request.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.SoPhong.Contains(keyword) || r.RoomType.Ten.Contains(keyword));
        }

        query = query.Where(r => !db.BookingDetails.Any(d => d.RoomId == r.RoomId &&
            d.Booking.TrangThai != "Cancelled" && d.Booking.TrangThai != "CheckedOut" &&
            d.Booking.NgayNhanPhong < to && from < d.Booking.NgayTraPhong));

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 48);
        var items = await query.OrderBy(r => r.RoomType.Gia).ThenBy(r => r.SoPhong)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RoomCardDto
            {
                RoomId = r.RoomId,
                SoPhong = r.SoPhong,
                Tang = r.Tang,
                TrangThai = r.TrangThai,
                DuongDanHinhAnh = r.DuongDanHinhAnh,
                RoomTypeId = r.RoomTypeId,
                LoaiPhong = r.RoomType.Ten,
                Gia = r.RoomType.Gia,
                SoKhachToiDa = r.RoomType.SoKhachToiDa,
                TienNghi = r.RoomType.TienNghi,
                ConTrong = true
            }).ToListAsync(ct);

        return new PagedResult<RoomCardDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<int> CreateBookingAsync(int userId, int? employeeId, CreateBookingRequest request, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.CheckIn < today) throw new ArgumentException("Ngày nhận phòng không được ở quá khứ.");
        if (request.CheckOut <= request.CheckIn) throw new ArgumentException("Ngày trả phòng phải sau ngày nhận phòng.");

        Customer? customer;
        if (employeeId.HasValue)
        {
            if (!request.CustomerId.HasValue) throw new ArgumentException("Nhân viên cần chọn khách hàng cho booking.");
            customer = await db.Customers.SingleOrDefaultAsync(c => c.CustomerId == request.CustomerId.Value, ct);
        }
        else
        {
            customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == userId, ct);
        }
        if (customer is null) throw new InvalidOperationException("Không tìm thấy hồ sơ khách hàng của booking.");
        var room = await db.Rooms.Include(r => r.RoomType).SingleOrDefaultAsync(r => r.RoomId == request.RoomId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy phòng được chọn.");
        if (request.Guests > room.RoomType.SoKhachToiDa) throw new ArgumentException("Số khách vượt quá sức chứa của phòng.");
        if (room.TrangThai is "Maintenance" or "NeedsCleaning") throw new InvalidOperationException("Phòng đang bảo trì hoặc chờ dọn dẹp.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var conflict = await db.BookingDetails.AnyAsync(d => d.RoomId == room.RoomId &&
            d.Booking.TrangThai != "Cancelled" && d.Booking.TrangThai != "CheckedOut" &&
            d.Booking.NgayNhanPhong < request.CheckOut && request.CheckIn < d.Booking.NgayTraPhong, ct);
        if (conflict) throw new InvalidOperationException("Phòng vừa được đặt bởi người khác trong khoảng thời gian này. Vui lòng tìm lại phòng.");

        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
        var booking = new Booking
        {
            NgayNhanPhong = request.CheckIn,
            NgayTraPhong = request.CheckOut,
            TrangThai = "Pending",
            TienCoc = 0,
            TongTien = room.RoomType.Gia * nights,
            CustomerId = customer.CustomerId,
            EmployeeId = employeeId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);
        db.BookingDetails.Add(new BookingDetail
        {
            BookingId = booking.BookingId,
            RoomId = room.RoomId,
            SoDem = nights,
            DonGia = room.RoomType.Gia
        });
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("Created booking {BookingId} for customer {CustomerId}, room {RoomId}", booking.BookingId, customer.CustomerId, room.RoomId);
            return booking.BookingId;
        }
        catch (Exception ex)
        {
            // The SQL trigger itself may have rolled back the transaction before raising its error.
            try { await tx.RollbackAsync(CancellationToken.None); }
            catch (Exception rollbackError) { logger.LogDebug(rollbackError, "Booking transaction was already rolled back by SQL Server."); }
            logger.LogWarning(ex, "Booking creation failed for room {RoomId}", room.RoomId);
            var errorText = ex.ToString();
            if (errorText.Contains("50001", StringComparison.OrdinalIgnoreCase) || errorText.Contains("50002", StringComparison.OrdinalIgnoreCase) || errorText.Contains("trùng", StringComparison.OrdinalIgnoreCase) || errorText.Contains("Phòng đã được đặt", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Phòng vừa phát sinh lịch đặt trùng. Vui lòng thử lại.");
            throw;
        }
    }

    public async Task CancelBookingAsync(int bookingId, int? customerId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(b => b.BookingDetails).SingleOrDefaultAsync(b => b.BookingId == bookingId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy booking.");
        if (customerId.HasValue && booking.CustomerId != customerId.Value)
            throw new UnauthorizedAccessException("Bạn không có quyền hủy booking này.");
        if (booking.TrangThai is "Cancelled" or "CheckedIn" or "CheckedOut")
            throw new InvalidOperationException("Booking ở trạng thái hiện tại không thể hủy.");
        booking.TrangThai = "Cancelled";
        booking.ThoiGianHuy = DateTime.Now;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Booking {BookingId} cancelled", bookingId);
    }
}
