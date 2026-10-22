using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQLKS.Models;
using WebsiteQLKS.Services;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Controllers.Api;

[ApiController]
[Route("api/bookings")]
public class BookingsApiController(HotelManagementDbContext db, IHotelService hotelService, ILogger<BookingsApiController> logger) : ControllerBase
{
    [Authorize(Roles = "Customer")]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var customerId = GetClaimId("CustomerId");
        var bookings = await db.Bookings.AsNoTracking().Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BookingId)
            .Select(b => new
            {
                b.BookingId, b.NgayNhanPhong, b.NgayTraPhong, b.TrangThai, b.TienCoc, b.TongTien,
                b.ThoiGianHuy, b.ThoiGianNhanPhongThucTe, b.ThoiGianTraPhongThucTe,
                Rooms = b.BookingDetails.Select(d => new { d.Room.SoPhong, LoaiPhong = d.Room.RoomType.Ten, d.SoDem, d.DonGia }),
                Payments = b.Payments.Select(p => new { p.PaymentId, p.SoTien, p.PhuongThuc, p.ThoiGianThanhToan, p.TrangThai })
            }).ToListAsync(ct);
        return Ok(bookings);
    }

    [Authorize(Roles = "Customer,Receptionist,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var userId = GetClaimId(ClaimTypes.NameIdentifier);
            var isCustomer = User.IsInRole("Customer");
            int? employeeId = isCustomer ? null : GetOptionalClaimId("EmployeeId");
            if (isCustomer && !GetOptionalClaimId("CustomerId").HasValue)
                return BadRequest(new { message = "Tài khoản chưa liên kết với hồ sơ khách hàng." });
            if (!isCustomer && !employeeId.HasValue)
                return BadRequest(new { message = "Tài khoản nhân viên chưa liên kết với hồ sơ Employee." });
            var id = await hotelService.CreateBookingAsync(userId, employeeId, request, ct);
            return Ok(new { bookingId = id, status = "Pending", message = "Đặt phòng thành công. Booking đang chờ xác nhận." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create booking");
            return StatusCode(500, new { message = "Không thể tạo booking lúc này. Vui lòng thử lại." });
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        try
        {
            await hotelService.CancelBookingAsync(id, GetClaimId("CustomerId"), ct);
            return Ok(new { message = "Đã hủy booking." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var customerId = GetClaimId("CustomerId");
        var booking = await db.Bookings.AsNoTracking().Where(b => b.BookingId == id && b.CustomerId == customerId)
            .Select(b => new { b.BookingId, b.NgayNhanPhong, b.NgayTraPhong, b.TrangThai, b.TongTien, b.TienCoc,
                Rooms = b.BookingDetails.Select(d => new { d.Room.SoPhong, LoaiPhong = d.Room.RoomType.Ten }),
                Payments = b.Payments.Select(p => new { p.SoTien, p.TrangThai, p.PhuongThuc, p.ThoiGianThanhToan }) })
            .SingleOrDefaultAsync(ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    private int GetClaimId(string type) => int.Parse(User.FindFirstValue(type) ?? "0");
    private int? GetOptionalClaimId(string type) => int.TryParse(User.FindFirstValue(type), out var id) ? id : null;
}
