CREATE DATABASE HotelManagementDb;
GO
USE HotelManagementDb;
GO
 
CREATE TABLE Role (
    RoleId  INT IDENTITY(1,1) PRIMARY KEY,
    Ten    NVARCHAR(30) NOT NULL,
    CONSTRAINT UQ_Role_Name UNIQUE (Ten)
);
 
CREATE TABLE [User] (
    UserId    INT IDENTITY(1,1) PRIMARY KEY,
    Username  NVARCHAR(50)  NOT NULL,
    Email     NVARCHAR(100) NOT NULL,
    MatKhau   NVARCHAR(256) NOT NULL,
    IsActive  BIT           NOT NULL CONSTRAINT DF_User_IsActive DEFAULT 1,
    RoleId    INT           NOT NULL,
    CONSTRAINT UQ_User_Username UNIQUE (Username),
    CONSTRAINT UQ_User_Email    UNIQUE (Email),
    CONSTRAINT FK_User_Role FOREIGN KEY (RoleId) REFERENCES Role(RoleId)
);
 
CREATE TABLE HotelInfo (
    HotelId       INT           NOT NULL PRIMARY KEY,
    Ten          NVARCHAR(150) NOT NULL,
    DiaChi       NVARCHAR(250) NOT NULL,
    SoDienThoai         NVARCHAR(20)  NOT NULL,
    Email         NVARCHAR(100) NULL,
    GioNhanPhong   TIME(0)       NOT NULL CONSTRAINT DF_HotelInfo_In  DEFAULT '14:00',
    GioTraPhong  TIME(0)       NOT NULL CONSTRAINT DF_HotelInfo_Out DEFAULT '12:00'
);
 
CREATE TABLE Employee (
    EmployeeId  INT IDENTITY(1,1) PRIMARY KEY,
    HoTen    NVARCHAR(100) NOT NULL,
    ChucVu    NVARCHAR(50)  NOT NULL,
    SoDienThoai       NVARCHAR(20)  NULL,
    IsActive    BIT           NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT 1,
    UserId      INT           NOT NULL,
    HotelId     INT           NOT NULL CONSTRAINT DF_Employee_HotelId DEFAULT 1,
    CONSTRAINT UQ_Employee_UserId UNIQUE (UserId),
    CONSTRAINT FK_Employee_User  FOREIGN KEY (UserId)  REFERENCES [User](UserId),
    CONSTRAINT FK_Employee_Hotel FOREIGN KEY (HotelId) REFERENCES HotelInfo(HotelId)
);
 
CREATE TABLE Customer (
    CustomerId  INT IDENTITY(1,1) PRIMARY KEY,
    HoTen    NVARCHAR(100) NOT NULL,
    SoGiayTo    NVARCHAR(20)  NULL,
    Email       NVARCHAR(100) NULL,
    SoDienThoai       NVARCHAR(20)  NOT NULL,
    UserId      INT           NULL,
    CONSTRAINT FK_Customer_User FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
CREATE UNIQUE INDEX UX_Customer_UserId   ON Customer(UserId)   WHERE UserId   IS NOT NULL;
CREATE UNIQUE INDEX UX_Customer_IdNumber ON Customer(SoGiayTo) WHERE SoGiayTo IS NOT NULL;
CREATE INDEX IX_Customer_Phone    ON Customer(SoDienThoai);
CREATE INDEX IX_Customer_FullName ON Customer(HoTen);
 
CREATE TABLE RoomType (
    RoomTypeId  INT IDENTITY(1,1) PRIMARY KEY,
    Ten        NVARCHAR(50)  NOT NULL,
    Gia       DECIMAL(18,2) NOT NULL,
    SoKhachToiDa    INT           NOT NULL,
    TienNghi   NVARCHAR(500) NULL,
    CONSTRAINT UQ_RoomType_Name UNIQUE (Ten),
    CONSTRAINT CK_RoomType_Price    CHECK (Gia >= 0),
    CONSTRAINT CK_RoomType_MaxGuest CHECK (SoKhachToiDa > 0)
);
 
CREATE TABLE Room (
    RoomId      INT IDENTITY(1,1) PRIMARY KEY,
    SoPhong  NVARCHAR(10)  NOT NULL,
    Tang       INT           NOT NULL,
    TrangThai      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Room_Status DEFAULT 'Available',
    DuongDanHinhAnh   NVARCHAR(260) NULL,
    RoomTypeId  INT           NOT NULL,
    HotelId     INT           NOT NULL CONSTRAINT DF_Room_HotelId DEFAULT 1,
    CONSTRAINT UQ_Room_RoomNumber UNIQUE (SoPhong),
    CONSTRAINT CK_Room_Status CHECK (TrangThai IN ('Available','Reserved','Occupied','Maintenance','NeedsCleaning')),
    CONSTRAINT FK_Room_RoomType FOREIGN KEY (RoomTypeId) REFERENCES RoomType(RoomTypeId),
    CONSTRAINT FK_Room_Hotel    FOREIGN KEY (HotelId)    REFERENCES HotelInfo(HotelId)
);
CREATE INDEX IX_Room_RoomTypeId ON Room(RoomTypeId);
CREATE INDEX IX_Room_Status     ON Room(TrangThai);
 
CREATE TABLE Booking (
    BookingId   INT IDENTITY(1,1) PRIMARY KEY,
    NgayNhanPhong     DATE          NOT NULL,
    NgayTraPhong    DATE          NOT NULL,
    TrangThai      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Booking_Status DEFAULT 'Pending',
    TienCoc     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Booking_Deposit DEFAULT 0,
    TongTien       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Booking_Total   DEFAULT 0,
    ThoiGianNhanPhongThucTe   DATETIME2(0) NULL,   -- thời điểm check-in thực tế
    ThoiGianTraPhongThucTe  DATETIME2(0) NULL,   -- thời điểm check-out thực tế
    ThoiGianHuy     DATETIME2(0) NULL,   -- thời điểm hủy đặt phòng
    CustomerId  INT           NOT NULL,
    EmployeeId  INT           NULL,
    CONSTRAINT CK_Booking_Dates  CHECK (NgayTraPhong > NgayNhanPhong),
    CONSTRAINT CK_Booking_ActualTimes CHECK (ThoiGianNhanPhongThucTe IS NULL OR ThoiGianTraPhongThucTe IS NULL OR ThoiGianTraPhongThucTe >= ThoiGianNhanPhongThucTe),
    CONSTRAINT CK_Booking_CancelledAt CHECK (ThoiGianHuy IS NULL OR TrangThai = 'Cancelled'),
    CONSTRAINT CK_Booking_Money  CHECK (TienCoc >= 0 AND TongTien >= 0),
    CONSTRAINT CK_Booking_Status CHECK (TrangThai IN ('Pending','Confirmed','CheckedIn','CheckedOut','Cancelled')),
    CONSTRAINT FK_Booking_Customer FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId),
    CONSTRAINT FK_Booking_Employee FOREIGN KEY (EmployeeId) REFERENCES Employee(EmployeeId)
);
CREATE INDEX IX_Booking_CustomerId ON Booking(CustomerId);
CREATE INDEX IX_Booking_Dates      ON Booking(NgayNhanPhong, NgayTraPhong) INCLUDE (TrangThai);
CREATE INDEX IX_Booking_Status     ON Booking(TrangThai);
 
CREATE TABLE BookingDetail (
    BookingDetailId  INT IDENTITY(1,1) PRIMARY KEY,
    SoDem           INT           NOT NULL,
    DonGia        DECIMAL(18,2) NOT NULL,
    BookingId        INT           NOT NULL,
    RoomId           INT           NOT NULL,
    CONSTRAINT UQ_BookingDetail_BookingRoom UNIQUE (BookingId, RoomId),
    CONSTRAINT CK_BookingDetail_Nights CHECK (SoDem > 0),
    CONSTRAINT CK_BookingDetail_Price  CHECK (DonGia >= 0),
    CONSTRAINT FK_BookingDetail_Booking FOREIGN KEY (BookingId) REFERENCES Booking(BookingId) ON DELETE CASCADE,
    CONSTRAINT FK_BookingDetail_Room    FOREIGN KEY (RoomId)    REFERENCES Room(RoomId)
);
CREATE INDEX IX_BookingDetail_RoomId ON BookingDetail(RoomId);
 
CREATE TABLE Invoice (
    InvoiceId  INT IDENTITY(1,1) PRIMARY KEY,
    SoDem     INT           NOT NULL,
    GiamGia   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoice_Discount  DEFAULT 0,
    PhuThu  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoice_Surcharge DEFAULT 0,
    TongTien      DECIMAL(18,2) NOT NULL,
    BookingId  INT           NOT NULL,
    CONSTRAINT UQ_Invoice_Booking UNIQUE (BookingId),
    CONSTRAINT CK_Invoice_Money CHECK (SoDem > 0 AND GiamGia >= 0 AND PhuThu >= 0 AND TongTien >= 0),
    CONSTRAINT FK_Invoice_Booking FOREIGN KEY (BookingId) REFERENCES Booking(BookingId)
);
 
CREATE TABLE Payment (
    PaymentId   INT IDENTITY(1,1) PRIMARY KEY,
    SoTien      DECIMAL(18,2) NOT NULL,
    PhuongThuc      NVARCHAR(20)  NOT NULL,
    ThoiGianThanhToan      DATETIME2(0)  NOT NULL CONSTRAINT DF_Payment_PaidAt DEFAULT SYSUTCDATETIME(),
    TrangThai      NVARCHAR(20)  NOT NULL CONSTRAINT DF_Payment_Status DEFAULT 'Completed',
    BookingId   INT           NOT NULL,
    InvoiceId   INT           NULL,
    EmployeeId  INT           NOT NULL,
    CONSTRAINT CK_Payment_Amount CHECK (SoTien > 0),
    CONSTRAINT CK_Payment_Method CHECK (PhuongThuc IN ('Cash','BankTransfer','Card')),
    CONSTRAINT CK_Payment_Status CHECK (TrangThai IN ('Completed','Refunded')),
    CONSTRAINT FK_Payment_Booking  FOREIGN KEY (BookingId)  REFERENCES Booking(BookingId),
    CONSTRAINT FK_Payment_Invoice  FOREIGN KEY (InvoiceId)  REFERENCES Invoice(InvoiceId),
    CONSTRAINT FK_Payment_Employee FOREIGN KEY (EmployeeId) REFERENCES Employee(EmployeeId)
);
CREATE INDEX IX_Payment_BookingId ON Payment(BookingId);
CREATE INDEX IX_Payment_PaidAt    ON Payment(ThoiGianThanhToan);
GO
 
CREATE VIEW vw_DailyRevenue AS
SELECT CAST(ThoiGianThanhToan AS DATE) AS [Date],
       SUM(CASE WHEN TrangThai = 'Completed' THEN SoTien ELSE -SoTien END) AS Revenue,
       COUNT(*) AS PaymentCount
FROM Payment
GROUP BY CAST(ThoiGianThanhToan AS DATE);
GO
 
CREATE VIEW vw_BookingSummary AS
SELECT b.BookingId, c.HoTen AS Customer, b.NgayNhanPhong, b.NgayTraPhong, b.TrangThai,
       COUNT(bd.BookingDetailId) AS RoomCount, b.TongTien, b.TienCoc
FROM Booking b
JOIN Customer c ON c.CustomerId = b.CustomerId
LEFT JOIN BookingDetail bd ON bd.BookingId = b.BookingId
GROUP BY b.BookingId, c.HoTen, b.NgayNhanPhong, b.NgayTraPhong, b.TrangThai, b.TongTien, b.TienCoc;
GO
 
CREATE VIEW vw_RoomStatusBoard AS
SELECT r.SoPhong, r.Tang, rt.Ten AS RoomType, rt.Gia, rt.SoKhachToiDa, r.TrangThai
FROM Room r
JOIN RoomType rt ON rt.RoomTypeId = r.RoomTypeId;
GO
 
INSERT INTO Role(Ten) VALUES ('Manager'), ('Receptionist'), ('Customer');
 
INSERT INTO HotelInfo(HotelId, Ten, DiaChi, SoDienThoai, Email, GioNhanPhong, GioTraPhong)
VALUES (1, N'Sunrise Hotel', N'25 Phố Huế, Hai Bà Trưng, Hà Nội', N'02438221234', N'contact@sunrisehotel.vn', '14:00', '12:00');
 
INSERT INTO RoomType(Ten, Gia, SoKhachToiDa, TienNghi) VALUES
 (N'Standard', 500000,  2, N'Wi-Fi, TV, Điều hòa, Tủ lạnh mini, Máy sấy tóc'),
 (N'Deluxe',   800000,  3, N'Wi-Fi, TV màn hình phẳng, Điều hòa, Minibar, Két sắt, Bàn làm việc'),
 (N'Family',   1200000, 4, N'Wi-Fi, TV, Điều hòa, Minibar, Bồn tắm, Ghế sofa'),
 (N'Suite',    2000000, 4, N'Wi-Fi, TV 55 inch, Điều hòa, Minibar, Bồn tắm nằm, Máy pha cà phê, Phòng khách riêng');
 
INSERT INTO Room(SoPhong, Tang, TrangThai, DuongDanHinhAnh, RoomTypeId, HotelId) VALUES 
('101', 1, 'Available', '/img/room-standard.jpg', 1, 1), 
('102', 1, 'Available', '/img/room-standard.jpg', 1, 1), 
('103', 1, 'Available', '/img/room-deluxe.jpg', 2, 1), 
('104', 1, 'Maintenance', '/img/room-deluxe.jpg', 2, 1), 
('105', 1, 'Available', '/img/room-standard.jpg', 1, 1), 
('201', 2, 'Available', '/img/room-deluxe.jpg', 2, 1), 
('202', 2, 'Available', '/img/room-deluxe.jpg', 2, 1), 
('203', 2, 'Occupied', '/img/room-family.jpg', 3, 1),  
('204', 2, 'Available', '/img/room-family.jpg', 3, 1),   
('205', 2, 'NeedsCleaning', '/img/room-standard.jpg', 1, 1), 
('301', 3, 'Occupied', '/img/room-family.jpg', 3, 1),  
('302', 3, 'Available', '/img/room-family.jpg', 3, 1), 
('303', 3, 'Available', '/img/room-suite.jpg', 4, 1), 
('304', 3, 'Available', '/img/room-suite.jpg', 4, 1),   
('305', 3, 'Available', '/img/room-deluxe.jpg', 2, 1), 
('401', 4, 'Available', '/img/room-suite.jpg', 4, 1), 
('402', 4, 'Available', '/img/room-suite.jpg', 4, 1), 
('403', 4, 'Available', '/img/room-family.jpg', 3, 1), 
('404', 4, 'Available', '/img/room-deluxe.jpg', 2, 1),   
('405', 4, 'Available', '/img/room-standard.jpg', 1, 1);

-- Phong bo sung de thu bo loc theo tang, loai phong va trang thai.
INSERT INTO Room(SoPhong, Tang, TrangThai, DuongDanHinhAnh, RoomTypeId, HotelId) VALUES
('406', 4, 'Available',      '/img/room-standard.jpg', 1, 1),
('407', 4, 'Available',      '/img/room-deluxe.jpg',   2, 1),
('408', 4, 'Available',      '/img/room-family.jpg',   3, 1),
('409', 4, 'Available',      '/img/room-suite.jpg',    4, 1),
('501', 5, 'Available',      '/img/room-deluxe.jpg',   2, 1),
('502', 5, 'Available',      '/img/room-suite.jpg',    4, 1);

INSERT INTO [User](Username, Email, MatKhau, RoleId) VALUES
 ('manager',    'manager@sunrisehotel.vn', 'AQAAAAIAAYagAAAAEOmgBRtS1dqoKgGxQSIak3BbjL+FZJpNeJbqQDqpUT/akx3rW5BQiRj2xwHKSPXE8A==', 1),
 ('letan01',    'letan01@sunrisehotel.vn', 'AQAAAAIAAYagAAAAEKsfgl3sMnwpsvkaKIg4Rnp0war7BAGNjhm+uG+j7g4RdW+CMQ3C0rSiLSYiEuprag==', 2),
 ('letan02',    'letan02@sunrisehotel.vn', 'AQAAAAIAAYagAAAAEKsfgl3sMnwpsvkaKIg4Rnp0war7BAGNjhm+uG+j7g4RdW+CMQ3C0rSiLSYiEuprag==', 2),
 ('nguyenvana', 'nguyenvana@gmail.com',    'AQAAAAIAAYagAAAAENG846BY2dcVr5HevZpLe7UVLTpj+KQOnUQyT3zCViZSQ/wE93+kzZM5zHA5xcWtkA==', 3);

-- TAI KHOAN MAU BO SUNG DE TEST: mat khau chung cho cac tai khoan nay la Test@123.
-- Hash duoc tao theo dinh dang ASP.NET Core Identity V3 (PBKDF2-HMAC-SHA512).
INSERT INTO [User](Username, Email, MatKhau, RoleId) VALUES
 ('tranthib', 'tranthib@gmail.com', 'AQAAAAIAAYagAAAAEE/IYDnUr5RTJ3rUy1+/seb1Ro/nkOuUMKjBDoVfstB9zwk213qiCSoowI9q8EJmhg==', 3),
 ('lehoangc', 'lehoangc@gmail.com', 'AQAAAAIAAYagAAAAENCy0RO0cq/gAAqZwBktqcRDwOD1443kNpWb1MJYlj/8ckPHi53xXVpjGMtrzfU2BQ==', 3),
 ('phamthid', 'phamthid@gmail.com', 'AQAAAAIAAYagAAAAEPRnCrpng+t6N+fNjpHUNk23KTguZrFRz8Pf3CDiPMBOiCnyIw3i7uBvIDW0KzZHfA==', 3),
 ('vuminhe', 'vuminhe@gmail.com', 'AQAAAAIAAYagAAAAENpSlTNHOkf6UUVr8k61zoHxXhA29lXudGo3HBPBbjvaDltODiv5pUDuo995/7Ja+w==', 3),
 ('hoangmylinh', 'hoangmylinh@gmail.com', 'AQAAAAIAAYagAAAAEMdlZTySPy7lN+f3jCrgCjJmntPOd8yn0M/TDN8Wkx4urj1D9Ygdu550n+rHBgNdrQ==', 3),
 ('dangquocbao', 'dangquocbao@gmail.com', 'AQAAAAIAAYagAAAAELKnUQaxFfxu7CGHKEufFuut8ww97qjdOMGW7ovN3VKsSIK8cUzCWaXX8OS/u/fOwQ==', 3),
 ('nguyengocmai', 'nguyengocmai@gmail.com', 'AQAAAAIAAYagAAAAEPL9FBSDdtUKfLT9gELcgNV9TZ6tB/BFwJ6i+5n54MBH8y++c2nULRCDHuj/+iRszA==', 3),
 ('truongducminh', 'truongducminh@gmail.com', 'AQAAAAIAAYagAAAAEHEQdWCP8PxfZ1G58/cUMJFjFam604DI68wTi8dRGQe449XEMX/ZnPQh5MK/0/Hr0g==', 3),
 ('phankhanhvy', 'phankhanhvy@gmail.com', 'AQAAAAIAAYagAAAAELL2dO5qr5848sXxeZz/USUOWInJ3HDqO29Yz8DZvie+VA2azRdRyCo7By97o/QxGg==', 3),
 ('letan03', 'letan03@sunrisehotel.vn', 'AQAAAAIAAYagAAAAEGkx7AZ1L2bCzzmUKUZ5iEuMzjnUFGmi9Pj/IZtTMk7DrO5QS1YIhpHINlJ+8ZGRsg==', 2);
 
INSERT INTO Employee(HoTen, ChucVu, SoDienThoai, UserId, HotelId) VALUES
 (N'Đặng Quốc Hùng',   N'Quản lý khách sạn', '0901000001', 1, 1),
 (N'Nguyễn Thu Trang', N'Lễ tân',            '0901000002', 2, 1),
 (N'Bùi Văn Khải',     N'Lễ tân',            '0901000003', 3, 1),
 (N'Phạm Ngọc Hà',     N'Lễ tân ca tối',     '0901000004', 14, 1);
 
INSERT INTO Customer(HoTen, SoGiayTo, Email, SoDienThoai, UserId) VALUES
 (N'Nguyễn Văn An',  '001090012345', 'nguyenvana@gmail.com', '0912000001', 4),
 (N'Trần Thị Bình',  '001092023456', 'tranthib@gmail.com',   '0912000002', 5),
 (N'Lê Hoàng Cường', '079088034567', 'lehoangc@gmail.com',   '0912000003', 6),
 (N'Phạm Thị Dung',  '048095045678', 'phamthid@gmail.com',   '0912000004', 7),
 (N'Vũ Minh Em',     '031087056789', 'vuminhe@gmail.com',    '0912000005', 8),
 (N'Hoàng Gia Huy',  '001085067890', NULL,                   '0912000006', NULL),
 (N'Đỗ Thị Lan',     '038093078901', 'dothilan@yahoo.com',   '0912000007', NULL);


-- Khach hang bo sung (CustomerId 8-12), lien ket voi tai khoan UserId 9-13.
INSERT INTO Customer(HoTen, SoGiayTo, Email, SoDienThoai, UserId) VALUES
 (N'Hoàng Mỹ Linh',  '001203111111', 'hoangmylinh@gmail.com',   '0912000008', 9),
 (N'Đặng Quốc Bảo',  '001203222222', 'dangquocbao@gmail.com',   '0912000009', 10),
 (N'Nguyễn Ngọc Mai','001203333333', 'nguyengocmai@gmail.com',  '0912000010', 11),
 (N'Trương Đức Minh','001203444444', 'truongducminh@gmail.com', '0912000011', 12),
 (N'Phan Khánh Vy',  '001203555555', 'phankhanhvy@gmail.com',   '0912000012', 13);
 
INSERT INTO Booking(NgayNhanPhong, NgayTraPhong, TrangThai, TienCoc, TongTien, ThoiGianNhanPhongThucTe, ThoiGianTraPhongThucTe, ThoiGianHuy, CustomerId, EmployeeId) VALUES
 ('2026-09-20', '2026-09-23', 'CheckedOut', 500000,  1500000, '2026-09-20T14:10:00', '2026-09-23T11:50:00', NULL, 1, 2),
 ('2026-10-01', '2026-10-05', 'CheckedIn',  1000000, 4800000, '2026-10-01T14:20:00', NULL, NULL, 2, 2),
 ('2026-10-02', '2026-10-04', 'CheckedIn',  600000,  2400000, '2026-10-02T15:00:00', NULL, NULL, 3, NULL),
 ('2026-10-10', '2026-10-13', 'Confirmed',  300000,  1500000, NULL, NULL, NULL, 4, NULL),
 ('2026-10-20', '2026-10-22', 'Pending',    0,       4000000, NULL, NULL, NULL, 5, 3),
 ('2026-09-28', '2026-09-30', 'Cancelled',  0,       1600000, NULL, NULL, '2026-09-26T16:30:00', 6, 3),
 ('2026-09-25', '2026-09-27', 'CheckedOut', 0,       2600000, '2026-09-25T14:30:00', '2026-09-27T12:00:00', NULL, 7, 2),
 ('2026-11-01', '2026-11-05', 'Confirmed',  2000000, 8000000, NULL, NULL, NULL, 1, 2);


-- Booking bo sung de test: cho duyet, sap nhan, dang o, da tra, da huy.
-- Su dung cac phong moi 406-502 (RoomId 21-26 tren database moi).
INSERT INTO Booking(NgayNhanPhong, NgayTraPhong, TrangThai, TienCoc, TongTien,
                    ThoiGianNhanPhongThucTe, ThoiGianTraPhongThucTe, ThoiGianHuy,
                    CustomerId, EmployeeId) VALUES
 ('2026-10-25', '2026-10-27', 'Confirmed',  500000, 1000000, NULL, NULL, NULL, 8, 2),
 ('2026-10-28', '2026-10-30', 'Pending',         0, 1600000, NULL, NULL, NULL, 9, 3),
 ('2026-10-20', '2026-10-24', 'CheckedIn',  800000, 4800000, '2026-10-20T14:10:00', NULL, NULL, 10, 2),
 ('2026-10-10', '2026-10-12', 'CheckedOut',1000000, 4000000, '2026-10-10T14:05:00', '2026-10-12T11:45:00', NULL, 11, 3),
 ('2026-10-05', '2026-10-07', 'Cancelled',       0, 1600000, NULL, NULL, '2026-10-04T16:30:00', 12, 3),
 ('2026-11-03', '2026-11-06', 'Confirmed',1500000, 6000000, NULL, NULL, NULL, 8, 4),
 ('2026-11-12', '2026-11-14', 'Pending',         0, 1000000, NULL, NULL, NULL, 12, 4);
 
INSERT INTO BookingDetail(SoDem, DonGia, BookingId, RoomId) VALUES
 (3, 500000,  1, 1),
 (4, 1200000, 2, 8),
 (2, 1200000, 3, 11),
 (3, 500000,  4, 2),
 (2, 2000000, 5, 13),
 (2, 800000,  6, 6),
 (2, 800000,  7, 7),
 (2, 500000,  7, 10),
 (4, 2000000, 8, 16);


INSERT INTO BookingDetail(SoDem, DonGia, BookingId, RoomId) VALUES
 (2,  500000,  9, 21),  -- 406 Standard
 (2,  800000, 10, 22),  -- 407 Deluxe
 (4, 1200000, 11, 23),  -- 408 Family
 (2, 2000000, 12, 24),  -- 409 Suite
 (2,  800000, 13, 25),  -- 501 Deluxe
 (3, 2000000, 14, 26),  -- 502 Suite
 (2,  500000, 15, 21);  -- 406 Standard, ngay khac booking 9
 
INSERT INTO Invoice(SoDem, GiamGia, PhuThu, TongTien, BookingId) VALUES
 (3, 50000, 100000, 1550000, 1),
 (2, 0,     0,      2600000, 7);

-- Hoa don bo sung cho booking da tra phong.
INSERT INTO Invoice(SoDem, GiamGia, PhuThu, TongTien, BookingId) VALUES
 (2, 0, 100000, 4100000, 12);
 
INSERT INTO Payment(SoTien, PhuongThuc, ThoiGianThanhToan, TrangThai, BookingId, InvoiceId, EmployeeId) VALUES
 (500000,  'Cash',         '2026-09-15T09:05:00', 'Completed', 1, NULL, 2),
 (1050000, 'Card',         '2026-09-23T11:40:00', 'Completed', 1, 1,    2),
 (1000000, 'BankTransfer', '2026-09-25T10:45:00', 'Completed', 2, NULL, 2),
 (600000,  'BankTransfer', '2026-09-28T20:30:00', 'Completed', 3, NULL, 3),
 (300000,  'BankTransfer', '2026-10-01T08:30:00', 'Completed', 4, NULL, 3),
 (2600000, 'Cash',         '2026-09-27T12:05:00', 'Completed', 7, 2,    2),
 (2000000, 'Card',         '2026-10-02T11:10:00', 'Completed', 8, NULL, 2);


INSERT INTO Payment(SoTien, PhuongThuc, ThoiGianThanhToan, TrangThai, BookingId, InvoiceId, EmployeeId) VALUES
 (500000,  'Cash',         '2026-10-22T09:15:00', 'Completed', 9,  NULL, 2),
 (1000000, 'BankTransfer', '2026-10-10T09:00:00', 'Completed', 12, 3,    3),
 (3100000, 'Card',         '2026-10-12T11:50:00', 'Completed', 12, 3,    3),
 (1500000, 'BankTransfer', '2026-10-21T14:00:00', 'Completed', 14, NULL, 4),
 (300000,  'Cash',         '2026-10-04T16:40:00', 'Refunded',  13, NULL, 3);
GO
CREATE OR ALTER TRIGGER trg_BookingDetail_NoOverlap
ON BookingDetail
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
 
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Booking b1       ON b1.BookingId = i.BookingId
        JOIN BookingDetail d2 ON d2.RoomId = i.RoomId
                             AND d2.BookingDetailId <> i.BookingDetailId
        JOIN Booking b2       ON b2.BookingId = d2.BookingId
        WHERE b1.TrangThai NOT IN ('Cancelled', 'CheckedOut')
          AND b2.TrangThai NOT IN ('Cancelled', 'CheckedOut')
          AND b1.NgayNhanPhong  < b2.NgayTraPhong
          AND b2.NgayNhanPhong  < b1.NgayTraPhong
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50001, N'Phòng đã được đặt trong khoảng thời gian này.', 1;
    END
END;
GO
 
CREATE OR ALTER TRIGGER trg_Booking_NoOverlap
ON Booking
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
 
    IF NOT (UPDATE(NgayNhanPhong) OR UPDATE(NgayTraPhong) OR UPDATE(TrangThai))
        RETURN;
 
    IF EXISTS (
        SELECT 1
        FROM inserted b1
        JOIN BookingDetail d1 ON d1.BookingId = b1.BookingId
        JOIN BookingDetail d2 ON d2.RoomId = d1.RoomId
                             AND d2.BookingId <> b1.BookingId
        JOIN Booking b2       ON b2.BookingId = d2.BookingId
        WHERE b1.TrangThai NOT IN ('Cancelled', 'CheckedOut')
          AND b2.TrangThai NOT IN ('Cancelled', 'CheckedOut')
          AND b1.NgayNhanPhong  < b2.NgayTraPhong
          AND b2.NgayNhanPhong  < b1.NgayTraPhong
    )
    BEGIN
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW 50002, N'Thay đổi làm trùng lịch với một booking khác trên cùng phòng.', 1;
    END
END;
GO
 
CREATE OR ALTER PROCEDURE sp_SearchAvailableRooms
    @NgayNhanPhong     DATE,
    @NgayTraPhong    DATE,
    @RoomTypeId  INT = NULL,
    @Guests      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
 
    IF @NgayTraPhong <= @NgayNhanPhong
        THROW 50003, N'Ngày trả phòng phải sau ngày nhận phòng.', 1;
 
    SELECT r.RoomId, r.SoPhong, r.Tang, r.DuongDanHinhAnh,
           rt.RoomTypeId, rt.Ten AS RoomType, rt.Gia, rt.SoKhachToiDa, rt.TienNghi
    FROM Room r
    JOIN RoomType rt ON rt.RoomTypeId = r.RoomTypeId
    WHERE r.TrangThai <> 'Maintenance'
      AND (@RoomTypeId IS NULL OR r.RoomTypeId = @RoomTypeId)
      AND (@Guests     IS NULL OR rt.SoKhachToiDa >= @Guests)
      AND NOT EXISTS (
            SELECT 1
            FROM BookingDetail d
            JOIN Booking b ON b.BookingId = d.BookingId
            WHERE d.RoomId = r.RoomId
              AND b.TrangThai NOT IN ('Cancelled', 'CheckedOut')
              AND b.NgayNhanPhong  < @NgayTraPhong
              AND @NgayNhanPhong   < b.NgayTraPhong
      )
    ORDER BY rt.Gia, r.SoPhong;
END;
GO