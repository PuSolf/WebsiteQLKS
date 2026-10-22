using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQLKS.Models;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Controllers.Api;

[ApiController]
[Route("api/management")]
[Authorize(Roles = "Manager,Receptionist")]
public class ManagementApiController(
    HotelManagementDbContext db,
    IPasswordHasher<User> passwordHasher,
    ILogger<ManagementApiController> logger,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("summary"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var totalRooms = await db.Rooms.CountAsync(ct);
        var availableRooms = await db.Rooms.CountAsync(r => r.TrangThai == "Available", ct);
        var activeBookings = await db.Bookings.CountAsync(b => b.TrangThai == "Pending" || b.TrangThai == "Confirmed" || b.TrangThai == "CheckedIn", ct);
        var todayArrivals = await db.Bookings.CountAsync(b => b.NgayNhanPhong == today && b.TrangThai != "Cancelled", ct);
        var todayDepartures = await db.Bookings.CountAsync(b => b.NgayTraPhong == today && b.TrangThai == "CheckedIn", ct);
        var monthlyRevenue = await db.Payments.Where(p => p.ThoiGianThanhToan >= today.ToDateTime(TimeOnly.MinValue).AddDays(1 - today.Day) && p.ThoiGianThanhToan < today.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .SumAsync(p => p.TrangThai == "Refunded" ? -p.SoTien : p.SoTien, ct);
        return Ok(new { totalRooms, availableRooms, activeBookings, todayArrivals, todayDepartures, monthlyRevenue });
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> Bookings([FromQuery] string? status, [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var query = db.Bookings.AsNoTracking().Include(b => b.Customer).Include(b => b.BookingDetails).ThenInclude(d => d.Room).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(b => b.TrangThai == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(b => b.Customer.HoTen.Contains(search) || b.BookingId.ToString() == search || b.BookingDetails.Any(d => d.Room.SoPhong.Contains(search)));
        }
        var total = await query.CountAsync(ct);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var items = await query.OrderByDescending(b => b.BookingId).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new
            {
                b.BookingId, b.NgayNhanPhong, b.NgayTraPhong, b.TrangThai, b.TienCoc, b.TongTien,
                b.ThoiGianNhanPhongThucTe, b.ThoiGianTraPhongThucTe, b.ThoiGianHuy,
                Customer = b.Customer.HoTen, b.Customer.SoDienThoai, b.Customer.Email,
                Rooms = b.BookingDetails.Select(d => new { d.RoomId, d.Room.SoPhong, RoomType = d.Room.RoomType.Ten, d.SoDem, d.DonGia })
            }).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpPost("bookings/{id:int}/confirm"), Authorize(Roles = "Manager,Receptionist"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.BookingId == id, ct);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        if (booking.TrangThai != "Pending") return Conflict(new { message = "Chỉ booking Pending mới được xác nhận." });
        booking.TrangThai = "Confirmed";
        booking.EmployeeId = GetEmployeeId() ?? booking.EmployeeId;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Booking {BookingId} confirmed by {User}", id, User.Identity?.Name);
        return Ok(new { message = "Đã xác nhận booking." });
    }

    [HttpPost("bookings/{id:int}/cancel"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.BookingId == id, ct);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        if (booking.TrangThai is "Cancelled" or "CheckedIn" or "CheckedOut") return Conflict(new { message = "Booking không thể hủy ở trạng thái hiện tại." });
        booking.TrangThai = "Cancelled"; booking.ThoiGianHuy = DateTime.Now;
        booking.EmployeeId = GetEmployeeId() ?? booking.EmployeeId;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã hủy booking." });
    }

    [HttpPost("bookings/{id:int}/check-in"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id, CancellationToken ct)
    {
        var booking = await db.Bookings.Include(b => b.BookingDetails).ThenInclude(d => d.Room).SingleOrDefaultAsync(b => b.BookingId == id, ct);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        if (booking.TrangThai != "Confirmed") return Conflict(new { message = "Chỉ booking Confirmed mới được check-in." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        booking.TrangThai = "CheckedIn"; booking.ThoiGianNhanPhongThucTe = DateTime.Now;
        booking.EmployeeId = GetEmployeeId() ?? booking.EmployeeId;
        foreach (var detail in booking.BookingDetails) detail.Room.TrangThai = "Occupied";
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        logger.LogInformation("Booking {BookingId} checked in", id);
        return Ok(new { message = "Check-in thành công." });
    }

    [HttpPost("bookings/{id:int}/check-out"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id, [FromBody] CheckoutRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var booking = await db.Bookings.Include(b => b.BookingDetails).ThenInclude(d => d.Room).Include(b => b.Invoice)
            .SingleOrDefaultAsync(b => b.BookingId == id, ct);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        if (booking.TrangThai != "CheckedIn") return Conflict(new { message = "Chỉ khách đang lưu trú mới được check-out." });
        if (request.GiamGia > booking.TongTien + request.PhuThu) return BadRequest(new { message = "Giảm giá không thể lớn hơn tổng tiền và phụ thu." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var nights = Math.Max(1, booking.NgayTraPhong.DayNumber - booking.NgayNhanPhong.DayNumber);
        var total = booking.TongTien - request.GiamGia + request.PhuThu;
        if (booking.Invoice is null)
        {
            booking.Invoice = new Invoice { BookingId = booking.BookingId, SoDem = nights, GiamGia = request.GiamGia, PhuThu = request.PhuThu, TongTien = total };
        }
        else
        {
            booking.Invoice.SoDem = nights; booking.Invoice.GiamGia = request.GiamGia; booking.Invoice.PhuThu = request.PhuThu; booking.Invoice.TongTien = total;
        }
        booking.TrangThai = "CheckedOut"; booking.ThoiGianTraPhongThucTe = DateTime.Now;
        booking.EmployeeId = GetEmployeeId() ?? booking.EmployeeId;
        foreach (var detail in booking.BookingDetails) detail.Room.TrangThai = "NeedsCleaning";
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        logger.LogInformation("Booking {BookingId} checked out; invoice total {Total}", id, total);
        return Ok(new { message = "Check-out thành công, hóa đơn đã được tạo/cập nhật.", invoiceTotal = total });
    }

    [HttpPut("bookings/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBooking(int id, [FromBody] UpdateBookingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.CheckOut <= request.CheckIn || request.CheckIn < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Ngày nhận phòng phải từ hôm nay và ngày trả phòng phải sau ngày nhận phòng." });
        var booking = await db.Bookings.Include(b => b.BookingDetails).SingleOrDefaultAsync(b => b.BookingId == id, ct);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        if (booking.TrangThai is not ("Pending" or "Confirmed")) return Conflict(new { message = "Chỉ booking Pending hoặc Confirmed mới được chỉnh sửa." });
        var room = await db.Rooms.Include(r => r.RoomType).SingleOrDefaultAsync(r => r.RoomId == request.RoomId, ct);
        if (room is null) return NotFound(new { message = "Không tìm thấy phòng." });
        if (room.TrangThai is "Maintenance" or "NeedsCleaning") return Conflict(new { message = "Phòng không sẵn sàng để đặt." });
        var conflict = await db.BookingDetails.AnyAsync(d => d.RoomId == request.RoomId && d.BookingId != id &&
            d.Booking.TrangThai != "Cancelled" && d.Booking.TrangThai != "CheckedOut" &&
            d.Booking.NgayNhanPhong < request.CheckOut && request.CheckIn < d.Booking.NgayTraPhong, ct);
        if (conflict) return Conflict(new { message = "Phòng bị trùng lịch trong khoảng ngày đã chọn." });
        if (booking.BookingDetails.Count == 0) return Conflict(new { message = "Booking chưa có chi tiết phòng." });
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
        var originalStatus = booking.TrangThai;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Temporarily exclude this booking while room detail and dates are updated; the trigger validates the final state when status is restored.
            booking.TrangThai = "Cancelled";
            await db.SaveChangesAsync(ct);
            var detail = booking.BookingDetails.First();
            detail.RoomId = room.RoomId; detail.SoDem = nights; detail.DonGia = room.RoomType.Gia;
            foreach (var extra in booking.BookingDetails.Skip(1)) db.BookingDetails.Remove(extra);
            await db.SaveChangesAsync(ct);
            booking.NgayNhanPhong = request.CheckIn; booking.NgayTraPhong = request.CheckOut;
            booking.TongTien = room.RoomType.Gia * nights; booking.EmployeeId = GetEmployeeId() ?? booking.EmployeeId;
            booking.TrangThai = originalStatus;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Ok(new { message = "Đã cập nhật booking." });
        }
        catch (DbUpdateException ex)
        {
            try { await tx.RollbackAsync(CancellationToken.None); }
            catch (Exception rollbackError) { logger.LogDebug(rollbackError, "Booking update transaction was already rolled back by SQL Server."); }
            logger.LogWarning(ex, "Could not update booking {BookingId}", id);
            return Conflict(new { message = "Không thể cập nhật booking; có thể trùng lịch hoặc dữ liệu không hợp lệ." });
        }
    }

    [HttpPost("rooms/image"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> UploadRoomImage( IFormFile? file, CancellationToken ct)
    {
        const long maxBytes = 3 * 1024 * 1024;
        if (file is null || file.Length == 0) return BadRequest(new { message = "Vui lòng chọn một ảnh." });
        if (file.Length > maxBytes) return BadRequest(new { message = "Ảnh không được vượt quá 3 MB." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            return BadRequest(new { message = "Chỉ nhận ảnh JPG, PNG hoặc WebP." });

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await input.ReadAsync(header.AsMemory(0, header.Length), ct);
        var validSignature = extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytesRead >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            _ => false
        };
        if (!validSignature) return BadRequest(new { message = "Nội dung file không khớp định dạng ảnh được chọn." });
        input.Position = 0;

        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRoot, "img", "rooms");
        Directory.CreateDirectory(folder);
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, safeFileName);
        await using (var output = System.IO.File.Create(fullPath))
        {
            await input.CopyToAsync(output, ct);
        }
        return Ok(new { path = $"/img/rooms/{safeFileName}", message = "Tải ảnh lên thành công." });
    }

    [HttpGet("rooms"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> Rooms(CancellationToken ct)
    {
        var items = await db.Rooms.AsNoTracking().Include(r => r.RoomType).OrderBy(r => r.Tang).ThenBy(r => r.SoPhong)
            .Select(r => new { r.RoomId, r.SoPhong, r.Tang, r.TrangThai, r.DuongDanHinhAnh, r.RoomTypeId, RoomType = r.RoomType.Ten, r.HotelId, Gia = r.RoomType.Gia })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost("rooms"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = new Room { SoPhong = request.SoPhong.Trim(), Tang = request.Tang, TrangThai = request.TrangThai, DuongDanHinhAnh = SafeLocalImagePath(request.DuongDanHinhAnh), RoomTypeId = request.RoomTypeId, HotelId = request.HotelId };
        db.Rooms.Add(entity);
        try { await db.SaveChangesAsync(ct); return Created($"/api/rooms/{entity.RoomId}", new { entity.RoomId }); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Could not create room"); return Conflict(new { message = "Không thể tạo phòng. Hãy kiểm tra số phòng, loại phòng và khách sạn." }); }
    }

    [HttpPut("rooms/{id:int}"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var room = await db.Rooms.SingleOrDefaultAsync(r => r.RoomId == id, ct);
        if (room is null) return NotFound(new { message = "Không tìm thấy phòng." });
        room.SoPhong = request.SoPhong.Trim(); room.Tang = request.Tang; room.TrangThai = request.TrangThai;
        room.DuongDanHinhAnh = SafeLocalImagePath(request.DuongDanHinhAnh); room.RoomTypeId = request.RoomTypeId; room.HotelId = request.HotelId;
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Đã cập nhật phòng." }); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Could not update room {RoomId}", id); return Conflict(new { message = "Không thể cập nhật phòng; hãy kiểm tra số phòng và loại phòng." }); }
    }

    [HttpDelete("rooms/{id:int}"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoom(int id, CancellationToken ct)
    {
        var room = await db.Rooms.SingleOrDefaultAsync(r => r.RoomId == id, ct);
        if (room is null) return NotFound(new { message = "Không tìm thấy phòng." });
        if (await db.BookingDetails.AnyAsync(d => d.RoomId == id, ct)) return Conflict(new { message = "Phòng đã có lịch sử booking nên không thể xóa. Hãy chuyển trạng thái sang Maintenance nếu ngừng khai thác." });
        db.Rooms.Remove(room); await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã xóa phòng." });
    }

    [HttpGet("room-types"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> RoomTypes(CancellationToken ct) => Ok(await db.RoomTypes.AsNoTracking().OrderBy(x => x.Gia)
        .Select(x => new { x.RoomTypeId, x.Ten, x.Gia, x.SoKhachToiDa, x.TienNghi }).ToListAsync(ct));

    [HttpPost("room-types"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRoomType([FromBody] CreateRoomTypeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = new RoomType { Ten = request.Ten.Trim(), Gia = request.Gia, SoKhachToiDa = request.SoKhachToiDa, TienNghi = request.TienNghi?.Trim() };
        db.RoomTypes.Add(entity);
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Đã tạo loại phòng.", entity.RoomTypeId }); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Could not create room type"); return Conflict(new { message = "Tên loại phòng bị trùng hoặc dữ liệu không hợp lệ." }); }
    }

    [HttpPut("room-types/{id:int}"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoomType(int id, [FromBody] CreateRoomTypeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = await db.RoomTypes.SingleOrDefaultAsync(x => x.RoomTypeId == id, ct);
        if (entity is null) return NotFound();
        entity.Ten = request.Ten.Trim(); entity.Gia = request.Gia; entity.SoKhachToiDa = request.SoKhachToiDa; entity.TienNghi = request.TienNghi?.Trim();
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Đã cập nhật loại phòng." }); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Could not update room type {Id}", id); return Conflict(new { message = "Không thể cập nhật loại phòng. Kiểm tra tên và dữ liệu." }); }
    }

    [HttpDelete("room-types/{id:int}"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoomType(int id, CancellationToken ct)
    {
        var entity = await db.RoomTypes.SingleOrDefaultAsync(x => x.RoomTypeId == id, ct);
        if (entity is null) return NotFound();
        if (await db.Rooms.AnyAsync(r => r.RoomTypeId == id, ct)) return Conflict(new { message = "Loại phòng đang được sử dụng bởi một hoặc nhiều phòng." });
        db.RoomTypes.Remove(entity); await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã xóa loại phòng." });
    }

    [HttpGet("customers"), Authorize(Roles = "Manager,Receptionist")]
    public async Task<IActionResult> Customers([FromQuery] string? q, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); query = query.Where(c => c.HoTen.Contains(term) || c.SoDienThoai.Contains(term) || (c.Email != null && c.Email.Contains(term))); }
        return Ok(await query.OrderBy(c => c.HoTen).Take(200).Select(c => new { c.CustomerId, c.HoTen, c.SoGiayTo, c.Email, c.SoDienThoai, c.UserId }).ToListAsync(ct));
    }

    [HttpPost("customers"), Authorize(Roles = "Manager,Receptionist"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.HoTen) || request.HoTen.Length > 100 || string.IsNullOrWhiteSpace(request.SoDienThoai) || request.SoDienThoai.Length > 20)
            return BadRequest(new { message = "Họ tên và số điện thoại là bắt buộc; kiểm tra độ dài dữ liệu." });
        var entity = new Customer { HoTen = request.HoTen.Trim(), SoDienThoai = request.SoDienThoai.Trim(), Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(), SoGiayTo = string.IsNullOrWhiteSpace(request.SoGiayTo) ? null : request.SoGiayTo.Trim() };
        db.Customers.Add(entity);
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Đã thêm khách hàng.", entity.CustomerId }); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Could not create customer"); return Conflict(new { message = "Không thể thêm khách hàng; có thể số giấy tờ đã tồn tại." }); }
    }

    [HttpPut("customers/{id:int}"), Authorize(Roles = "Manager,Receptionist"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.CustomerId == id, ct);
        if (customer is null) return NotFound();
        var normalizedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        if (customer.UserId.HasValue && normalizedEmail is null)
            return BadRequest(new { message = "Khách đã có tài khoản phải có email." });
        if (normalizedEmail is not null && await db.Users.AnyAsync(u => u.Email == normalizedEmail && u.UserId != (customer.UserId ?? 0), ct))
            return Conflict(new { message = "Email đã được tài khoản khác sử dụng." });
        customer.HoTen = request.HoTen.Trim(); customer.SoDienThoai = request.SoDienThoai.Trim();
        customer.Email = normalizedEmail;
        customer.SoGiayTo = string.IsNullOrWhiteSpace(request.SoGiayTo) ? null : request.SoGiayTo.Trim();
        if (customer.UserId.HasValue && normalizedEmail is not null)
        {
            var linkedUser = await db.Users.SingleAsync(u => u.UserId == customer.UserId.Value, ct);
            linkedUser.Email = normalizedEmail;
        }
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Đã cập nhật khách hàng." }); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Could not update customer {CustomerId}", id); return Conflict(new { message = "Email hoặc số giấy tờ đã tồn tại." }); }
    }

    [HttpDelete("customers/{id:int}"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.CustomerId == id, ct);
        if (customer is null) return NotFound();
        if (customer.UserId.HasValue || await db.Bookings.AnyAsync(b => b.CustomerId == id, ct)) return Conflict(new { message = "Khách hàng đã có tài khoản hoặc lịch sử booking, không thể xóa." });
        db.Customers.Remove(customer); await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã xóa khách hàng." });
    }

    [HttpGet("employees"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> Employees(CancellationToken ct) => Ok(await db.Employees.AsNoTracking().Include(e => e.User)
        .Where(e => e.User.Role.Ten == "Receptionist")
        .OrderBy(e => e.HoTen).Select(e => new { e.EmployeeId, e.HoTen, e.ChucVu, e.SoDienThoai, e.IsActive, e.HotelId, e.User.Username, e.User.Email }).ToListAsync(ct));

    [HttpPost("employees"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await db.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email, ct)) return Conflict(new { message = "Tên đăng nhập hoặc email đã tồn tại." });
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Ten == "Receptionist", ct);
        if (role is null) return BadRequest(new { message = "Database chưa có role Receptionist." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = new User { Username = request.Username.Trim(), Email = request.Email.Trim().ToLowerInvariant(), RoleId = role.RoleId, IsActive = true };
        user.MatKhau = passwordHasher.HashPassword(user, request.Password);
        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            db.Employees.Add(new Employee { HoTen = request.HoTen.Trim(), ChucVu = request.ChucVu, SoDienThoai = request.SoDienThoai, IsActive = true, UserId = user.UserId, HotelId = request.HotelId });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Ok(new { message = "Đã tạo tài khoản lễ tân.", userId = user.UserId });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogWarning(ex, "Could not create employee account");
            return Conflict(new { message = "Không thể tạo tài khoản nhân viên; kiểm tra HotelId và thông tin tài khoản." });
        }
    }

    [HttpPost("employees/{id:int}/toggle-status"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEmployeeStatus(int id, CancellationToken ct)
    {
        var employee = await db.Employees.Include(e => e.User).ThenInclude(u => u.Role).SingleOrDefaultAsync(e => e.EmployeeId == id, ct);
        if (employee is null) return NotFound(new { message = "Không tìm thấy nhân viên." });
        if (employee.User.Role.Ten != "Receptionist") return Conflict(new { message = "Chỉ có thể khóa/mở khóa tài khoản lễ tân từ màn hình này." });
        employee.IsActive = !employee.IsActive; employee.User.IsActive = employee.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = employee.IsActive ? "Đã mở khóa tài khoản nhân viên." : "Đã khóa tài khoản nhân viên.", employee.IsActive });
    }

    [HttpGet("payments"), Authorize(Roles = "Manager,Receptionist")]
    public async Task<IActionResult> Payments([FromQuery] int? bookingId, CancellationToken ct)
    {
        var query = db.Payments.AsNoTracking().Include(p => p.Booking).ThenInclude(b => b.Customer).Include(p => p.Employee).AsQueryable();
        if (bookingId.HasValue) query = query.Where(p => p.BookingId == bookingId.Value);
        return Ok(await query.OrderByDescending(p => p.ThoiGianThanhToan).Take(300).Select(p => new
        {
            p.PaymentId, p.BookingId, Customer = p.Booking.Customer.HoTen, p.SoTien, p.PhuongThuc, p.TrangThai,
            p.ThoiGianThanhToan, p.InvoiceId, NhanVien = p.Employee.HoTen
        }).ToListAsync(ct));
    }

    [HttpPost("payments"), Authorize(Roles = "Manager,Receptionist"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue) return BadRequest(new { message = "Tài khoản nhân viên chưa liên kết EmployeeId." });
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.BookingId == request.BookingId, ct);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        if (request.InvoiceId.HasValue && !await db.Invoices.AnyAsync(i => i.InvoiceId == request.InvoiceId.Value && i.BookingId == booking.BookingId, ct))
            return BadRequest(new { message = "Invoice không thuộc booking được chọn." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Payments.Add(new Payment { BookingId = booking.BookingId, InvoiceId = request.InvoiceId, EmployeeId = employeeId.Value, SoTien = request.SoTien, PhuongThuc = request.PhuongThuc, TrangThai = "Completed", ThoiGianThanhToan = DateTime.UtcNow });
        if (!request.InvoiceId.HasValue) booking.TienCoc += request.SoTien;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        logger.LogInformation("Payment registered for booking {BookingId} amount {Amount}", booking.BookingId, request.SoTien);
        return Ok(new { message = "Đã ghi nhận thanh toán.", booking.TienCoc });
    }

    [HttpPost("payments/{id:int}/refund"), Authorize(Roles = "Manager,Receptionist"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int id, CancellationToken ct)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(p => p.PaymentId == id, ct);
        if (payment is null) return NotFound(new { message = "Không tìm thấy giao dịch." });
        if (payment.TrangThai != "Completed") return Conflict(new { message = "Chỉ giao dịch Completed mới được hoàn tiền." });
        payment.TrangThai = "Refunded";
        if (payment.InvoiceId is null)
        {
            var booking = await db.Bookings.SingleAsync(b => b.BookingId == payment.BookingId, ct);
            booking.TienCoc = Math.Max(0m, booking.TienCoc - payment.SoTien);
        }
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Payment {PaymentId} refunded", id);
        return Ok(new { message = "Đã ghi nhận hoàn tiền." });
    }

    [HttpGet("reports"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> Reports(CancellationToken ct)
    {
        var revenue = await db.VwDailyRevenues.OrderByDescending(x => x.Date).Take(30).ToListAsync(ct);
        var bookings = await db.VwBookingSummaries.OrderByDescending(x => x.BookingId).Take(100).ToListAsync(ct);
        return Ok(new { revenue, bookings });
    }

    [HttpGet("hotel-info"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> HotelInfo(CancellationToken ct)
    {
        var hotel = await db.HotelInfos.AsNoTracking().OrderBy(h => h.HotelId).FirstOrDefaultAsync(ct);
        return hotel is null ? NotFound() : Ok(hotel);
    }

    [HttpPut("hotel-info/{id:int}"), Authorize(Roles = "Manager"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateHotelInfo(int id, [FromBody] UpdateHotelInfoRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var hotel = await db.HotelInfos.SingleOrDefaultAsync(h => h.HotelId == id, ct);
        if (hotel is null) return NotFound();
        hotel.Ten = request.Ten.Trim(); hotel.DiaChi = request.DiaChi.Trim(); hotel.SoDienThoai = request.SoDienThoai.Trim();
        hotel.Email = request.Email?.Trim(); hotel.GioNhanPhong = request.GioNhanPhong; hotel.GioTraPhong = request.GioTraPhong;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Đã cập nhật thông tin khách sạn." });
    }

    private int? GetEmployeeId() => int.TryParse(User.FindFirstValue("EmployeeId"), out var id) ? id : null;
    private static string? SafeLocalImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var normalized = path.Trim().Replace('\\', '/');
        if (!normalized.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) || normalized.Contains("..") || normalized.Length > 260) return null;
        return normalized;
    }
}
