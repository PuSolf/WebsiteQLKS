using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQLKS.Models;
using WebsiteQLKS.Services;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Controllers.Api;

[ApiController]
[Route("api/rooms")]
public class RoomsApiController(HotelManagementDbContext db, IHotelService hotelService, ILogger<RoomsApiController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoomCardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] RoomSearchRequest request, CancellationToken ct)
    {
        try { return Ok(await hotelService.SearchRoomsAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var room = await db.Rooms.AsNoTracking().Include(r => r.RoomType).Include(r => r.Hotel)
            .Where(r => r.RoomId == id)
            .Select(r => new
            {
                r.RoomId, r.SoPhong, r.Tang, r.TrangThai, r.DuongDanHinhAnh,
                r.RoomTypeId, LoaiPhong = r.RoomType.Ten, r.RoomType.Gia, r.RoomType.SoKhachToiDa, r.RoomType.TienNghi,
                KhachSan = r.Hotel.Ten, DiaChi = r.Hotel.DiaChi
            }).SingleOrDefaultAsync(ct);
        return room is null ? NotFound(new { message = "Không tìm thấy phòng." }) : Ok(room);
    }

    [HttpGet("types")]
    public async Task<IActionResult> RoomTypes(CancellationToken ct)
    {
        var types = await db.RoomTypes.AsNoTracking().OrderBy(t => t.Gia)
            .Select(t => new { t.RoomTypeId, t.Ten, t.Gia, t.SoKhachToiDa, t.TienNghi }).ToListAsync(ct);
        return Ok(types);
    }
}
