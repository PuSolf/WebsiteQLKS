using System.ComponentModel.DataAnnotations;

namespace WebsiteQLKS.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản.")]
    public string UsernameOrEmail { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength = 2)] public string HoTen { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(20)] public string SoDienThoai { get; set; } = "";
    [Required, StringLength(50, MinimumLength = 4)] public string Username { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.") , DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}

public sealed class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string HoTen { get; set; } = "";
    [Required, Phone, StringLength(20)] public string SoDienThoai { get; set; } = "";
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [StringLength(20)] public string? SoGiayTo { get; set; }
}
