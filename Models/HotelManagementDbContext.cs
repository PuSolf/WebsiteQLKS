using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebsiteQLKS.Models;

public partial class HotelManagementDbContext : DbContext
{
    public HotelManagementDbContext()
    {
    }

    public HotelManagementDbContext(DbContextOptions<HotelManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<HotelInfo> HotelInfos { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwBookingSummary> VwBookingSummaries { get; set; }

    public virtual DbSet<VwDailyRevenue> VwDailyRevenues { get; set; }

    public virtual DbSet<VwRoomStatusBoard> VwRoomStatusBoards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Booking__73951AED4A9E23A5");

            entity.ToTable("Booking", tb => tb.HasTrigger("trg_Booking_NoOverlap"));

            entity.HasIndex(e => e.CustomerId, "IX_Booking_CustomerId");

            entity.HasIndex(e => new { e.NgayNhanPhong, e.NgayTraPhong }, "IX_Booking_Dates");

            entity.HasIndex(e => e.TrangThai, "IX_Booking_Status");

            entity.Property(e => e.ThoiGianHuy).HasPrecision(0);
            entity.Property(e => e.ThoiGianNhanPhongThucTe).HasPrecision(0);
            entity.Property(e => e.ThoiGianTraPhongThucTe).HasPrecision(0);
            entity.Property(e => e.TienCoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Pending", "DF_Booking_Status");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Customer");

            entity.HasOne(d => d.Employee).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Booking_Employee");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => e.BookingDetailId).HasName("PK__BookingD__8136D45A0531FFC3");

            entity.ToTable("BookingDetail", tb => tb.HasTrigger("trg_BookingDetail_NoOverlap"));

            entity.HasIndex(e => e.RoomId, "IX_BookingDetail_RoomId");

            entity.HasIndex(e => new { e.BookingId, e.RoomId }, "UQ_BookingDetail_BookingRoom").IsUnique();

            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_BookingDetail_Booking");

            entity.HasOne(d => d.Room).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_Room");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D8EC3AE813");

            entity.ToTable("Customer");

            entity.HasIndex(e => e.HoTen, "IX_Customer_FullName");

            entity.HasIndex(e => e.SoDienThoai, "IX_Customer_Phone");

            entity.HasIndex(e => e.SoGiayTo, "UX_Customer_IdNumber")
                .IsUnique()
                .HasFilter("([SoGiayTo] IS NOT NULL)");

            entity.HasIndex(e => e.UserId, "UX_Customer_UserId")
                .IsUnique()
                .HasFilter("([UserId] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.SoGiayTo).HasMaxLength(20);

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .HasConstraintName("FK_Customer_User");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F11D56A6530");

            entity.ToTable("Employee");

            entity.HasIndex(e => e.UserId, "UQ_Employee_UserId").IsUnique();

            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.HotelId).HasDefaultValue(1, "DF_Employee_HotelId");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Employee_IsActive");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);

            entity.HasOne(d => d.Hotel).WithMany(p => p.Employees)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employee_Hotel");

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employee_User");
        });

        modelBuilder.Entity<HotelInfo>(entity =>
        {
            entity.HasKey(e => e.HotelId).HasName("PK__HotelInf__46023BDF03EB4537");

            entity.ToTable("HotelInfo");

            entity.Property(e => e.HotelId).ValueGeneratedNever();
            entity.Property(e => e.DiaChi).HasMaxLength(250);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GioNhanPhong)
                .HasPrecision(0)
                .HasDefaultValue(new TimeOnly(14, 0, 0), "DF_HotelInfo_In");
            entity.Property(e => e.GioTraPhong)
                .HasPrecision(0)
                .HasDefaultValue(new TimeOnly(12, 0, 0), "DF_HotelInfo_Out");
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.Ten).HasMaxLength(150);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoice__D796AAB5FBEECFDC");

            entity.ToTable("Invoice");

            entity.HasIndex(e => e.BookingId, "UQ_Invoice_Booking").IsUnique();

            entity.Property(e => e.GiamGia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PhuThu).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Booking).WithOne(p => p.Invoice)
                .HasForeignKey<Invoice>(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Booking");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A38227AFF83");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.BookingId, "IX_Payment_BookingId");

            entity.HasIndex(e => e.ThoiGianThanhToan, "IX_Payment_PaidAt");

            entity.Property(e => e.PhuongThuc).HasMaxLength(20);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThoiGianThanhToan)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Payment_PaidAt");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Completed", "DF_Payment_Status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Booking");

            entity.HasOne(d => d.Employee).WithMany(p => p.Payments)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Employee");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Payment_Invoice");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1AF298C226");

            entity.ToTable("Role");

            entity.HasIndex(e => e.Ten, "UQ_Role_Name").IsUnique();

            entity.Property(e => e.Ten).HasMaxLength(30);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.RoomId).HasName("PK__Room__32863939FC3D3EF8");

            entity.ToTable("Room");

            entity.HasIndex(e => e.RoomTypeId, "IX_Room_RoomTypeId");

            entity.HasIndex(e => e.TrangThai, "IX_Room_Status");

            entity.HasIndex(e => e.SoPhong, "UQ_Room_RoomNumber").IsUnique();

            entity.Property(e => e.DuongDanHinhAnh).HasMaxLength(260);
            entity.Property(e => e.HotelId).HasDefaultValue(1, "DF_Room_HotelId");
            entity.Property(e => e.SoPhong).HasMaxLength(10);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValue("Available", "DF_Room_Status");

            entity.HasOne(d => d.Hotel).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.HotelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_Hotel");

            entity.HasOne(d => d.RoomType).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.RoomTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Room_RoomType");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.RoomTypeId).HasName("PK__RoomType__BCC89631EF50209A");

            entity.ToTable("RoomType");

            entity.HasIndex(e => e.Ten, "UQ_RoomType_Name").IsUnique();

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Ten).HasMaxLength(50);
            entity.Property(e => e.TienNghi).HasMaxLength(500);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4CA4644DDD");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ_User_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_User_Username").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_User_IsActive");
            entity.Property(e => e.MatKhau).HasMaxLength(256);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<VwBookingSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_BookingSummary");

            entity.Property(e => e.Customer).HasMaxLength(100);
            entity.Property(e => e.TienCoc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrangThai).HasMaxLength(20);
        });

        modelBuilder.Entity<VwDailyRevenue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_DailyRevenue");

            entity.Property(e => e.Revenue).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwRoomStatusBoard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RoomStatusBoard");

            entity.Property(e => e.Gia).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RoomType).HasMaxLength(50);
            entity.Property(e => e.SoPhong).HasMaxLength(10);
            entity.Property(e => e.TrangThai).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
