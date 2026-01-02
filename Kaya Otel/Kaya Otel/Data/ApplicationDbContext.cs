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
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<user> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

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
                
                // Foreign key ilişkisi (TourId)
                entity.HasOne<Tour>()
                    .WithMany()
                    .HasForeignKey(e => e.TourId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Sifre).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.UserName).IsUnique();
            });

            // User yapılandırması
            modelBuilder.Entity<user>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(200);
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

            // Seed data - İlk admin kullanıcısı
            modelBuilder.Entity<Admin>().HasData(
                new Admin { Id = 1, UserName = "admin", Sifre = "123" }
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

