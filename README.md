# Hotel Management System (HMS) — Sunrise Hotel

Project ASP.NET Core MVC + Web API + EF Core + SQL Server, dựng từ project mẫu và schema SQL trong tài liệu M2/M3.

## 1. Công nghệ

- ASP.NET Core MVC (.NET 10), Razor Views.
- EF Core SQL Server; giữ nguyên các entity đã scaffold trong `Models` và `HotelManagementDbContext`.
- Cookie Authentication, role-based authorization (`Manager`, `Receptionist`, `Customer`).
- REST API tại `/api/...`; Swagger UI ở `/swagger` trong môi trường Development.
- AJAX/fetch API cập nhật danh sách phòng, xem chi tiết, gửi yêu cầu booking, tải booking, cập nhật vận hành mà không reload toàn trang.

## 2. Mở project trong Visual Studio

1. Cài .NET 10 SDK và workload ASP.NET and web development cho Visual Studio.
2. Mở `WebsiteQLKS.csproj` (hoặc thêm project vào solution của bạn).
3. Trong SQL Server Management Studio, chạy **một lần** file `Models/csdl.sql`. File này đã bao gồm database `HotelManagementDb`, 11 bảng, views, triggers, stored procedure và dữ liệu mẫu theo schema đính kèm.
4. Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.json` theo SQL Server của máy. Ví dụ Windows Authentication:

   `Server=YOUR_SQL_SERVER;Database=HotelManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`

   Hoặc đặt connection string qua biến môi trường `ConnectionStrings__DefaultConnection`. Không commit connection string chứa mật khẩu thật vào Git.
5. Chọn HTTPS profile và Run. Trang chính hiển thị tại `/`; docs API tại `/swagger` khi chạy Development.
6. Khôi phục NuGet packages nếu Visual Studio yêu cầu. Máy hiện tại khi tạo gói không có `dotnet` SDK, nên project chưa thể được build/run end-to-end trong môi trường tạo file; hãy build lần đầu trong Visual Studio sau khi cấu hình SQL.

## 3. Tài khoản demo từ SQL seed

Trong `csdl.sql`, các giá trị `MatKhau` mẫu là MD5 của `1`, `2`, `3` tương ứng (mặc dù comment seed trong SQL ghi chung một mật khẩu). Vì vậy, với database nguyên bản:

| Vai trò | Username | Mật khẩu ban đầu |
|---|---|---|
| Manager | `manager` | `1` |
| Receptionist | `letan01` | `2` |
| Customer | `nguyenvana` | `3` |

Đây chỉ là dữ liệu demo. Code hỗ trợ xác minh MD5 legacy đúng một lần rồi nâng cấp mật khẩu sang `PasswordHasher<User>` của ASP.NET Core sau khi đăng nhập thành công. Hãy đổi mật khẩu demo và không dùng seed account trong môi trường production.

## 4. Cấu trúc thư mục nổi bật

```text
WebsiteQLKS/
├── WebsiteQLKS.sln            # Mở trực tiếp bằng Visual Studio
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── ManagementController.cs
│   └── Api/
│       ├── RoomsApiController.cs
│       ├── BookingsApiController.cs
│       ├── ManagementApiController.cs
│       └── CustomerApiController.cs
├── Models/                     # Entity EF Core scaffold + DbContext
│   ├── HotelManagementDbContext.cs
│   ├── [các entity từ SQL]
│   └── csdl.sql                 # Chỉ một file SQL trong project
├── Services/HotelService.cs     # Kiểm tra phòng và nghiệp vụ booking
├── ViewModels/                  # Input/DTO và ErrorViewModel, không trùng entity DB
├── Views/                       # MVC Razor views
└── wwwroot/
    ├── css/site.css
    ├── js/                      # AJAX/fetch scripts
    └── img/                     # Hình minh họa dạng SVG cục bộ
```

## 5. Tính năng có trong project

- Trang chủ phong cách tối giản, tông xanh rêu/kem/vàng champagne, responsive desktop/mobile.
- Tìm phòng theo ngày nhận/trả, hạng phòng, số khách, từ khóa và giá tối đa; phân trang; chi tiết phòng trong modal; tất cả tải qua AJAX.
- Booking nhanh phía Customer, trạng thái ban đầu `Pending`; xác thực ngày, sức chứa và lịch đặt trùng; ghi nhận thời điểm hủy.
- Đăng ký/đăng nhập/đăng xuất, mật khẩu mới được hash, role được cấp từ DB, cookie HttpOnly, kiểm tra quyền phía server.
- Customer xem booking, giao dịch và cập nhật hồ sơ.
- Receptionist/Manager tìm booking, confirm/cancel, check-in/check-out; check-in ghi thời điểm thực tế, checkout tạo/cập nhật invoice discount/surcharge và chuyển phòng sang `NeedsCleaning`.
- Ghi nhận deposit/payment và refund; quản lý phòng và room type; quản lý customer; tạo tài khoản lễ tân; cập nhật thông tin khách sạn; báo cáo doanh thu/booking từ views có sẵn.
- Tải ảnh phòng có kiểm tra phần mở rộng, chữ ký file và giới hạn 3 MB; ảnh được đặt trong `wwwroot/img/rooms` với tên ngẫu nhiên. Form thanh toán tại quầy nhận mã hóa đơn tùy chọn để phân biệt khoản đặt cọc với khoản thanh toán hóa đơn.
- CSRF protection cho các POST/PUT/DELETE AJAX bằng anti-forgery token; kiểm tra dữ liệu server-side; logging các thao tác và lỗi quan trọng.

## 6. API tiêu biểu

| Method | Endpoint | Mục đích / quyền |
|---|---|---|
| GET | `/api/rooms` | Tìm phòng trống theo filter; public |
| GET | `/api/rooms/{id}` | Chi tiết phòng; public |
| GET | `/api/rooms/types` | Danh sách hạng phòng; public |
| POST | `/api/bookings` | Đặt phòng nhanh; đã đăng nhập |
| GET | `/api/bookings/mine` | Booking cá nhân; Customer |
| POST | `/api/bookings/{id}/cancel` | Khách tự hủy booking của mình; Customer |
| GET | `/api/management/bookings` | Tra cứu booking; Manager/Receptionist |
| POST | `/api/management/bookings/{id}/confirm` | Xác nhận; Manager/Receptionist |
| POST | `/api/management/bookings/{id}/check-in` | Check-in; Manager/Receptionist |
| POST | `/api/management/bookings/{id}/check-out` | Check-out + hóa đơn; Manager/Receptionist |
| CRUD | `/api/management/rooms` | Quản lý phòng; Manager |
| POST | `/api/management/rooms/image` | Tải ảnh JPG/PNG/WebP giới hạn 3 MB; Manager |
| CRUD | `/api/management/room-types` | Quản lý hạng phòng; Manager |
| GET/POST | `/api/management/payments` | Tra cứu / ghi nhận thanh toán; Manager/Receptionist |
| POST | `/api/management/payments/{id}/refund` | Ghi nhận hoàn tiền; Manager/Receptionist |
| GET | `/api/management/reports` | Doanh thu + booking summary; Manager |
| GET/PUT | `/api/management/hotel-info/{id}` | Thông tin khách sạn; Manager |

## 7. Kiểm thử thủ công trước khi demo

1. Đăng nhập với từng role, thử mở URL quản trị bằng Customer để xác minh server trả 403.
2. Tìm phòng với ngày hợp lệ; thay đổi ngày/hạng/số khách/từ khóa/giá và xác minh chỉ lưới phòng được cập nhật.
3. Customer đặt một phòng, kiểm tra booking có trạng thái `Pending`; thử đặt cùng phòng cùng khoảng ngày lần nữa và xác minh request bị từ chối.
4. Receptionist xác nhận booking, check-in; kiểm tra `ThoiGianNhanPhongThucTe` và `Room.TrangThai`.
5. Check-out với discount/surcharge; kiểm tra Invoice, `ThoiGianTraPhongThucTe` và trạng thái `NeedsCleaning`.
6. Ghi nhận deposit/payment; thử refund hai lần để xác minh chỉ payment `Completed` được refund.
7. Thử ngày check-out trước/ngang ngày check-in, giá min lớn hơn max, dữ liệu trùng và thao tác không có quyền.

## 8. Ghi chú và giới hạn hiện tại

- Project được tổ chức theo schema M2/M3 và giữ nguyên bộ entity scaffold hiện có. Không có file migration mới; script SQL đã là cách setup database có thể tái lập theo yêu cầu môn học.
- API tạo booking nhận `Note` để tương thích form nhưng schema hiện tại không có cột lưu ghi chú nên trường đó không được ghi xuống DB.
- Luồng thanh toán là ghi nhận nghiệp vụ nội bộ, chưa kết nối cổng thanh toán bên thứ ba.
- Quản lý ảnh hiện dùng các file SVG minh họa có sẵn trong `wwwroot/img`; nếu thêm ảnh upload, cần thêm API upload với allow-list MIME/extension và giới hạn dung lượng trước khi triển khai production.
- Triggers SQL là lớp bảo vệ bổ sung cho lịch trùng; API cũng kiểm tra trước khi tạo booking. Các thay đổi đồng thời vẫn cần test trên instance SQL Server thật.
