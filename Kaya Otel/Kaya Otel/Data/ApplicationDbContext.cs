using Kaya_Otel.Models;
using Microsoft.EntityFrameworkCore;

namespace Kaya_Otel.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tour> Tours { get; set; }
        public DbSet<DeletedTour> DeletedTours { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CancelledBooking> CancelledBookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<user> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tour yapılandırması
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.PricePerPerson).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
            });

            // DeletedTour yapılandırması
            modelBuilder.Entity<DeletedTour>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.PricePerPerson).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.DeletedAt).IsRequired();
                entity.Property(e => e.DeletedBy).HasMaxLength(100);
            });

            // CancelledBooking yapılandırması
            modelBuilder.Entity<CancelledBooking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TourName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CancelledAt).IsRequired();
                entity.Property(e => e.CancelledBy).HasMaxLength(100);
                entity.Property(e => e.CancellationReason).HasMaxLength(500);
            });

            // Booking yapılandırması
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TourName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DepositAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UserId).IsRequired(false);
                entity.Property(e => e.CancellationRequestReason).HasMaxLength(500);
                
                // Foreign key ilişkisi (TourId)
                entity.HasOne<Tour>()
                    .WithMany()
                    .HasForeignKey(e => e.TourId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Foreign key ilişkisi (UserId) - opsiyonel
                entity.HasOne<user>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Payment yapılandırması
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Provider).HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.TransactionId).HasMaxLength(200);
                
                // Foreign key ilişkisi (BookingId)
                entity.HasOne<Booking>()
                    .WithMany()
                    .HasForeignKey(e => e.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Admin yapılandırması
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Email ve Name kolonları veritabanında yoksa hata vermemesi için opsiyonel yapıyoruz
                // Program.cs'de bu kolonlar otomatik eklenecek
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.Sifre).IsRequired().HasMaxLength(200);
                // Index'i sadece kolon varsa oluştur
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt);
            });

            // User yapılandırması
            modelBuilder.Entity<user>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Email ve Name kolonları veritabanında yoksa hata vermemesi için opsiyonel yapıyoruz
                // Program.cs'de bu kolonlar otomatik eklenecek
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(200);
                // Index'i sadece kolon varsa oluştur
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt);
            });

            // Room yapılandırması
            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

                // Reservation yapılandırması
                modelBuilder.Entity<Reservation>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.RoomName).HasMaxLength(200);
                    entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                    entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                });

                // Notification yapılandırması
                modelBuilder.Entity<Notification>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                    entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
                    entity.Property(e => e.Type).HasMaxLength(50);
                    entity.Property(e => e.CancellationReason).HasMaxLength(500);
                    entity.Property(e => e.CreatedAt).IsRequired();
                });

            // Seed data - İlk admin kullanıcısı
            modelBuilder.Entity<Admin>().HasData(
                new Admin 
                { 
                    Id = 1, 
                    Name = "Admin",
                    Email = "admin@kekovatur.com",
                    UserName = "admin", 
                    Sifre = "123",
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed data - İlk turlar
            modelBuilder.Entity<Tour>().HasData(
                new Tour
                {
                    Id = 1,
                    Name = "Özel Kekova Günbatımı Turu",
                    Category = "Günbatımı",
                    Description = "Gün batımını Kekovanın koylarında karşılayan tekne turu.",
                    PricePerPerson = 15000,
                    Capacity = 12,
                    ImageUrl = "/images/sunset.jpg",
                    IsActive = true
                },
                new Tour
                {
                    Id = 2,
                    Name = "Mehtap Turu",
                    Category = "Mehtap",
                    Description = "Gece ışıkları altında Kekovanın sakin sularında mehtap turu.",
                    PricePerPerson = 12000,
                    Capacity = 12,
                    ImageUrl = "/images/kekova1.jpg",
                    IsActive = true
                },
                new Tour
                {
                    Id = 3,
                    Name = "Günlük Yemekli Özel Tekne Turu",
                    Category = "Tam Günlük",
                    Description = "Koy koy gezerek kekovanın tarihi doğasında akdeniz lezzetlerinin de sizlere eşlik ettiği tur keyfi.",
                    PricePerPerson = 20000,
                    Capacity = 12,
                    Duration = TimeSpan.FromHours(7),
                    ImageUrl = "/images/günlükturfoto.jpg",
                    IsActive = true
                }
            );
        }
    }
}

