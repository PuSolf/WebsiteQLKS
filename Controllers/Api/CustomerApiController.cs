using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQLKS.Models;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Controllers.Api;

[ApiController]
[Route("api/customer")]
[Authorize(Roles = "Customer")]
public class CustomerApiController(HotelManagementDbContext db) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var customer = await db.Customers.AsNoTracking().Where(c => c.UserId == userId)
            .Select(c => new { c.CustomerId, c.HoTen, c.SoGiayTo, c.Email, c.SoDienThoai, Username = c.User!.Username })
            .SingleOrDefaultAsync(ct);
        return customer is null ? NotFound(new { message = "Không tìm thấy hồ sơ khách hàng." }) : Ok(customer);
    }

    [HttpPut("profile"), ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.UserId == userId, ct);
        if (customer is null) return NotFound(new { message = "Không tìm thấy hồ sơ khách hàng." });
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email là bắt buộc đối với tài khoản đã đăng ký." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email && u.UserId != userId, ct))
            return Conflict(new { message = "Email đã được tài khoản khác sử dụng." });
        customer.HoTen = request.HoTen.Trim(); customer.SoDienThoai = request.SoDienThoai.Trim();
        customer.Email = email; customer.SoGiayTo = string.IsNullOrWhiteSpace(request.SoGiayTo) ? null : request.SoGiayTo.Trim();
        var user = await db.Users.SingleAsync(u => u.UserId == userId, ct);
        user.Email = email;
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Đã cập nhật hồ sơ." }); }
        catch (DbUpdateException) { return Conflict(new { message = "Không thể cập nhật email hoặc số giấy tờ do dữ liệu đã tồn tại." }); }
    }
}
