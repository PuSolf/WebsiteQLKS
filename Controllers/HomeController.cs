using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebsiteQLKS.Models;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Controllers;

public class HomeController(HotelManagementDbContext db, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var hotel = await db.HotelInfos.AsNoTracking().FirstOrDefaultAsync(ct);
        var roomTypes = await db.RoomTypes.AsNoTracking().OrderBy(r => r.Gia).ToListAsync(ct);
        ViewBag.Hotel = hotel;
        ViewBag.RoomTypes = roomTypes;
        return View();
    }

    [Authorize(Roles = "Customer")]
    public IActionResult MyBookings() => View();

    [Authorize(Roles = "Customer")]
    public IActionResult Profile() => View();

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        logger.LogError("Unhandled request error. TraceId={TraceId}", requestId);
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
