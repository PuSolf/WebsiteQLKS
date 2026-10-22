# Test cases — Hotel Management System

> Các ca kiểm thử dưới đây là kịch bản đề xuất để chạy trong Visual Studio/SQL Server. Cột **Actual result** và **Pass/Fail** được để trống cho nhóm điền sau khi chạy thật; không xem đây là bằng chứng kiểm thử đã thực thi.

| ID | Test objective | Input / precondition | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|---|
| TC-01 | Đăng nhập đúng thông tin | `manager` và mật khẩu demo `1` trên DB seed nguyên bản | Đăng nhập thành công, role Manager | | |
| TC-02 | Từ chối thông tin đăng nhập sai | Username hoặc mật khẩu sai | Không cấp session; hiện thông báo lỗi phù hợp | | |
| TC-03 | Phân quyền server | Đăng nhập Customer rồi gửi GET `/api/management/bookings` | HTTP 403 hoặc chuyển luồng đăng nhập nếu chưa xác thực; không lộ dữ liệu quản trị | | |
| TC-04 | Tìm phòng bằng AJAX | Ngày nhận/trả hợp lệ, thay đổi filter hạng phòng/số khách/từ khóa | Chỉ vùng kết quả phòng được cập nhật; không reload trang | | |
| TC-05 | Validate ngày | Check-out trước hoặc bằng check-in | API trả lỗi validation; không tạo booking | | |
| TC-06 | Đặt phòng thành công | Customer đăng nhập, phòng trống, số khách hợp lệ | Tạo Booking `Pending` và BookingDetail | | |
| TC-07 | Ngăn đặt trùng | Gửi hai booking cùng phòng và khoảng ngày giao nhau | Booking thứ hai bị từ chối, dữ liệu không ghi trùng | | |
| TC-08 | Hủy booking thuộc khách khác | Customer A gửi yêu cầu hủy booking của Customer B | Server từ chối; booking không thay đổi | | |
| TC-09 | Confirm và check-in | Receptionist chọn booking `Pending`, xác nhận rồi check-in | Trạng thái lần lượt Confirmed/CheckedIn; ghi nhận thời gian thực tế; phòng Occupied | | |
| TC-10 | Check-out tạo hóa đơn | Booking đang CheckedIn, nhập discount/surcharge hợp lệ | Invoice được tạo/cập nhật; booking CheckedOut; phòng NeedsCleaning | | |
| TC-11 | Ghi nhận deposit/payment | Receptionist nhập khoản tiền với số tiền dương/phương thức hợp lệ | Payment được ghi nhận theo trạng thái schema | | |
| TC-12 | Refund chỉ một lần | Gửi refund với Payment Completed, sau đó gửi refund lần hai | Lần đầu thành công; lần sau bị từ chối vì trạng thái không còn Completed | | |
| TC-13 | Quản lý room type | Manager thêm, sửa và xóa loại phòng chưa được sử dụng | API trả kết quả phù hợp; danh sách cập nhật qua AJAX | | |
| TC-14 | Thử CSRF protection | Gửi POST/PUT/DELETE không có anti-forgery token hợp lệ | Request bị từ chối | | |
| TC-15 | Responsive | Mở trang chủ, booking và dashboard ở kích thước điện thoại/desktop | Điều khiển vẫn dùng được; không có tràn ngang không mong muốn | | |
| TC-16 | Upload ảnh phòng an toàn | Manager tải file JPG/PNG/WebP đúng định dạng ≤ 3 MB rồi thử file sai định dạng/quá cỡ | Ảnh hợp lệ lưu trong `wwwroot/img/rooms`; file sai hoặc quá lớn bị từ chối | | |

## Quy trình ghi nhận

1. Chạy `Models/csdl.sql` trên một database phát triển riêng và cập nhật connection string.
2. Chạy ứng dụng ở môi trường Development, dùng tài khoản demo trong README.
3. Ghi kết quả thực tế, mã HTTP/chụp màn hình và Pass/Fail cho từng test.
4. Với test thất bại, bổ sung defect, cách tái hiện, nguyên nhân và hướng sửa vào báo cáo cuối kỳ.
