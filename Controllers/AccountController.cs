using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteQLKS.Models;
using WebsiteQLKS.ViewModels;

namespace WebsiteQLKS.Controllers;

public class AccountController(
    HotelManagementDbContext db,
    IPasswordHasher<User> passwordHasher,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var login = model.UsernameOrEmail.Trim();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(
            u => u.Username == login || u.Email == login, ct);
        if (user is null || !user.IsActive || !await CheckAndUpgradePasswordAsync(user, model.Password, ct))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản, mật khẩu không đúng hoặc tài khoản đã bị khóa.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.Ten)
        };
        if (user.Role.Ten == "Customer")
        {
            var customerId = await db.Customers.Where(c => c.UserId == user.UserId).Select(c => (int?)c.CustomerId).FirstOrDefaultAsync(ct);
            if (customerId.HasValue) claims.Add(new Claim("CustomerId", customerId.Value.ToString()));
        }
        else
        {
            var employeeId = await db.Employees.Where(e => e.UserId == user.UserId && e.IsActive).Select(e => (int?)e.EmployeeId).FirstOrDefaultAsync(ct);
            if (employeeId.HasValue) claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
        logger.LogInformation("User {UserId} signed in with role {Role}", user.UserId, user.Role.Ten);
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl);
        return user.Role.Ten switch
        {
            "Manager" => RedirectToAction("Index", "Management"),
            "Receptionist" => RedirectToAction("Reception", "Management"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        var username = model.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username || u.Email == email, ct))
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc email đã được sử dụng.");
            return View(model);
        }
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Ten == "Customer", ct);
        if (role is null) { ModelState.AddModelError(string.Empty, "Cơ sở dữ liệu chưa có role Customer."); return View(model); }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = new User { Username = username, Email = email, RoleId = role.RoleId, IsActive = true, MatKhau = "" };
        user.MatKhau = passwordHasher.HashPassword(user, model.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        db.Customers.Add(new Customer { HoTen = model.HoTen.Trim(), Email = email, SoDienThoai = model.SoDienThoai.Trim(), UserId = user.UserId });
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogWarning(ex, "Customer registration violated a database constraint");
            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản. Hãy kiểm tra email/tên đăng nhập và thử lại.");
            return View(model);
        }
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task<bool> CheckAndUpgradePasswordAsync(User user, string password, CancellationToken ct)
    {
        if (user.MatKhau.Length == 32 && user.MatKhau.All(Uri.IsHexDigit))
        {
            var legacyHash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(legacyHash), Encoding.UTF8.GetBytes(user.MatKhau.ToLowerInvariant()))) return false;
            user.MatKhau = passwordHasher.HashPassword(user, password);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Migrated legacy MD5 password for user {UserId} to ASP.NET Core PasswordHasher", user.UserId);
            return true;
        }
        var result = passwordHasher.VerifyHashedPassword(user, user.MatKhau, password);
        if (result == PasswordVerificationResult.Failed) return false;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.MatKhau = passwordHasher.HashPassword(user, password);
            await db.SaveChangesAsync(ct);
        }
        return true;
    }
}
