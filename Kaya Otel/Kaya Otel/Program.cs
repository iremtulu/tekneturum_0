using Kaya_Otel.Data;
using Kaya_Otel.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Database yapılandırması - MSSQL + EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' bulunamadı.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    
    // Development ortamında detaylı hata mesajları
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});


builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IIyzicoPaymentService, IyzicoPaymentService>();
// Instagram servisini yapılandır - token varsa gerçek API, yoksa fake servis kullan
var instagramToken = builder.Configuration["Instagram:AccessToken"];
if (!string.IsNullOrEmpty(instagramToken))
{
    builder.Services.AddScoped<IInstagramService, InstagramGraphService>();
}
else
{
    builder.Services.AddScoped<IInstagramService, FakeInstagramService>();
}
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
  options.IdleTimeout = TimeSpan.FromMinutes(30);
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Veritabanını otomatik oluştur ve seed data ekle (ilk çalıştırmada)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Veritabanı bağlantısını test et
        var canConnect = dbContext.Database.CanConnect();
        logger.LogInformation($"Veritabanı bağlantısı: {(canConnect ? "Başarılı" : "Başarısız")}");
        
        if (!canConnect)
        {
            logger.LogError("❌ Veritabanına bağlanılamıyor. Lütfen veritabanı bağlantı ayarlarını kontrol edin.");
            throw new Exception("Veritabanı bağlantısı başarısız.");
        }
        
        // Tabloların var olup olmadığını kontrol et
        bool tablesExist = false;
        try
        {
            // Tours tablosuna basit bir sorgu yapmayı dene
            var test = dbContext.Tours.Count();
            tablesExist = true;
            logger.LogInformation("Tablolar mevcut.");
        }
        catch
        {
            logger.LogWarning("Tablolar bulunamadı. Oluşturuluyor...");
            tablesExist = false;
        }
        
        // Tablolar yoksa oluştur
        if (!tablesExist)
        {
            var created = dbContext.Database.EnsureCreated();
            if (created)
            {
                logger.LogInformation("✅ Veritabanı ve tablolar oluşturuldu.");
            }
            else
            {
                logger.LogWarning("⚠️ Tablolar oluşturulamadı.");
            }
        }
        
        // Admins tablosuna Email, Name ve CreatedAt kolonlarını ekle (eğer yoksa)
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Admins') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Admins') AND name = 'Email')
                    BEGIN
                        ALTER TABLE dbo.Admins ADD Email nvarchar(200) NULL;
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Admins') AND name = 'UserName')
                        BEGIN
                            UPDATE dbo.Admins SET Email = UserName + '@kekovatur.com' WHERE Email IS NULL;
                        END
                        ELSE
                        BEGIN
                            UPDATE dbo.Admins SET Email = 'admin@kekovatur.com' WHERE Email IS NULL;
                        END
                        ALTER TABLE dbo.Admins ALTER COLUMN Email nvarchar(200) NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Admins_Email' AND object_id = OBJECT_ID(N'dbo.Admins'))
                        BEGIN
                            CREATE UNIQUE INDEX IX_Admins_Email ON dbo.Admins(Email);
                        END
                        PRINT 'Admins.Email kolonu eklendi.';
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Admins') AND name = 'Name')
                    BEGIN
                        ALTER TABLE dbo.Admins ADD Name nvarchar(200) NULL;
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Admins') AND name = 'UserName')
                        BEGIN
                            UPDATE dbo.Admins SET Name = UserName WHERE Name IS NULL;
                        END
                        ELSE
                        BEGIN
                            UPDATE dbo.Admins SET Name = 'Admin' WHERE Name IS NULL;
                        END
                        ALTER TABLE dbo.Admins ALTER COLUMN Name nvarchar(200) NOT NULL;
                        PRINT 'Admins.Name kolonu eklendi.';
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Admins') AND name = 'CreatedAt')
                    BEGIN
                        ALTER TABLE dbo.Admins ADD CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
                        PRINT 'Admins.CreatedAt kolonu eklendi.';
                    END
                END
            ");
            logger.LogInformation("✅ Admins tablosu kolonları kontrol edildi/eklendi.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Admins tablosu kolonları eklenirken hata: {ex.Message}");
        }

        // Users tablosuna Email ve CreatedAt kolonlarını ekle (eğer yoksa)
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Users') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'Email')
                    BEGIN
                        ALTER TABLE dbo.Users ADD Email nvarchar(200) NULL;
                        UPDATE dbo.Users SET Email = Name + '@example.com' WHERE Email IS NULL;
                        ALTER TABLE dbo.Users ALTER COLUMN Email nvarchar(200) NOT NULL;
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
                        BEGIN
                            CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
                        END
                        PRINT 'Users.Email kolonu eklendi.';
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = 'CreatedAt')
                    BEGIN
                        ALTER TABLE dbo.Users ADD CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
                        PRINT 'Users.CreatedAt kolonu eklendi.';
                    END
                END
            ");
            logger.LogInformation("✅ Users tablosu kolonları kontrol edildi/eklendi.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Users tablosu kolonları eklenirken hata: {ex.Message}");
        }

        // Bookings tablosuna UserId kolonunu ekle (eğer yoksa)
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Bookings') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Bookings') AND name = 'UserId')
                    BEGIN
                        ALTER TABLE dbo.Bookings ADD UserId int NULL;
                        PRINT 'Bookings.UserId kolonu eklendi.';
                    END
                END
            ");
            logger.LogInformation("✅ Bookings.UserId kolonu kontrol edildi/eklendi.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Bookings.UserId kolonu eklenirken hata: {ex.Message}");
        }

        // DeletedTours ve CancelledBookings tablolarının varlığını kontrol et ve oluştur
        try
        {
            // DeletedTours tablosunu kontrol et
            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeletedTours]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[DeletedTours] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [OriginalTourId] int NOT NULL,
                        [Name] nvarchar(200) NOT NULL,
                        [Category] nvarchar(100) NULL,
                        [Description] nvarchar(1000) NULL,
                        [PricePerPerson] decimal(18,2) NOT NULL,
                        [Capacity] int NOT NULL,
                        [Duration] time NOT NULL,
                        [ImageUrl] nvarchar(500) NULL,
                        [IsActive] bit NOT NULL,
                        [DeletedAt] datetime2 NOT NULL,
                        [DeletedBy] nvarchar(100) NULL,
                        CONSTRAINT [PK_DeletedTours] PRIMARY KEY ([Id])
                    );
                END
            ");
            logger.LogInformation("✅ DeletedTours tablosu kontrol edildi/oluşturuldu.");
            
            // Mevcut DeletedTours kayıtlarının DeletedAt saatlerini UTC'den Turkey Time'a (UTC+3) güncelle
            // Tüm kayıtları güncelle (UTC olarak kaydedilmiş olanları Turkey Time'a çevir)
            try
            {
                var updatedRows = dbContext.Database.ExecuteSqlRaw(@"
                    UPDATE [dbo].[DeletedTours]
                    SET [DeletedAt] = DATEADD(HOUR, 3, [DeletedAt])
                    WHERE [DeletedAt] < DATEADD(HOUR, 3, GETUTCDATE())
                       OR [DeletedAt] < DATEADD(HOUR, -3, GETDATE())
                ");
                if (updatedRows > 0)
                {
                    logger.LogInformation($"✅ DeletedTours tablosundaki {updatedRows} kaydın saat bilgisi güncellendi (UTC -> Turkey Time).");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"DeletedTours saat güncellemesi sırasında hata: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"DeletedTours tablosu kontrol edilirken hata: {ex.Message}");
        }

        try
        {
            // CancelledBookings tablosunu kontrol et
            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CancelledBookings]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[CancelledBookings] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [OriginalBookingId] int NOT NULL,
                        [TourId] int NOT NULL,
                        [TourName] nvarchar(200) NOT NULL,
                        [TourDate] datetime2 NOT NULL,
                        [Guests] int NOT NULL,
                        [CustomerName] nvarchar(200) NOT NULL,
                        [UserId] int NULL,
                        [Email] nvarchar(200) NULL,
                        [Phone] nvarchar(50) NULL,
                        [TotalAmount] decimal(18,2) NOT NULL,
                        [DepositAmount] decimal(18,2) NOT NULL,
                        [IsDepositPaid] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CancelledAt] datetime2 NOT NULL,
                        [CancelledBy] nvarchar(100) NULL,
                        [CancellationReason] nvarchar(500) NULL,
                        CONSTRAINT [PK_CancelledBookings] PRIMARY KEY ([Id])
                    );
                END
                
                -- UserId kolonu yoksa ekle
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CancelledBookings]') AND name = 'UserId')
                BEGIN
                    ALTER TABLE [dbo].[CancelledBookings] ADD [UserId] int NULL;
                END
            ");
            logger.LogInformation("✅ CancelledBookings tablosu kontrol edildi/oluşturuldu.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"CancelledBookings tablosu kontrol edilirken hata: {ex.Message}");
        }

        try
        {
            // Bookings tablosuna iptal talebi kolonlarını ekle
            dbContext.Database.ExecuteSqlRaw(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'CancellationRequested')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [CancellationRequested] bit NOT NULL DEFAULT 0;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'CancellationRequestReason')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [CancellationRequestReason] nvarchar(500) NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'CancellationRequestedAt')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [CancellationRequestedAt] datetime2 NULL;
                    END
                END
            ");
            logger.LogInformation("✅ Bookings tablosu iptal talebi kolonları kontrol edildi/eklendi.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Bookings tablosu iptal talebi kolonları eklenirken hata: {ex.Message}");
        }

        try
        {
            // Bookings tablosuna güncelleme talebi kolonlarını ekle
            dbContext.Database.ExecuteSqlRaw(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND type in (N'U'))
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'UpdateRequested')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [UpdateRequested] bit NOT NULL DEFAULT 0;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'UpdateRequestReason')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [UpdateRequestReason] nvarchar(500) NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'UpdateRequestedAt')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [UpdateRequestedAt] datetime2 NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'UpdateRequestStatus')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [UpdateRequestStatus] nvarchar(50) NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'AdminUpdateResponse')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [AdminUpdateResponse] nvarchar(500) NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'RequestedTourDate')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [RequestedTourDate] datetime2 NULL;
                    END
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND name = 'RequestedGuests')
                    BEGIN
                        ALTER TABLE [dbo].[Bookings] ADD [RequestedGuests] int NULL;
                    END
                END
            ");
            logger.LogInformation("✅ Bookings tablosu güncelleme talebi kolonları kontrol edildi/eklendi.");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Bookings tablosu güncelleme talebi kolonları eklenirken hata: {ex.Message}");
        }

        try
        {
            // Notifications tablosunu kontrol et
            dbContext.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[Notifications] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [Title] nvarchar(200) NOT NULL,
                        [Message] nvarchar(1000) NOT NULL,
                        [Type] nvarchar(50) NULL DEFAULT 'info',
                        [UserId] int NULL,
                        [AdminId] int NULL,
                        [IsRead] bit NOT NULL DEFAULT 0,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [RelatedBookingId] int NULL,
                        [CancellationReason] nvarchar(500) NULL,
                        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
                    );
                END
            ");
            logger.LogInformation("✅ Notifications tablosu kontrol edildi/oluşturuldu.");
            }
        catch (Exception ex)
        {
            logger.LogWarning($"Notifications tablosu kontrol edilirken hata: {ex.Message}");
        }

        
        // Seed data ekle (eğer yoksa)
        try
        {
            // Email ile kontrol et (yeni yapı)
            var adminCount = 0;
            try
            {
                adminCount = dbContext.Admins.Count();
            }
            catch (Exception ex) when (ex.Message.Contains("Invalid column name 'Email'") || ex.Message.Contains("Invalid column name 'Name'"))
            {
                // Kolonlar henüz eklenmemiş, UserName ile kontrol et
                logger.LogWarning("Email/Name kolonları henüz yok, UserName ile kontrol ediliyor...");
                try
                {
                    // Raw SQL ile kontrol et
                    using var connection = dbContext.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM Admins";
                    var result = command.ExecuteScalar();
                    adminCount = result != null ? Convert.ToInt32(result) : 0;
                }
                catch
                {
                    adminCount = 0;
                }
            }
            
            logger.LogInformation($"Mevcut admin sayısı: {adminCount}");
            
            if (adminCount == 0)
            {
                // Önce kolonların varlığını kontrol et
                bool hasEmail = false;
                bool hasName = false;
                try
                {
                    var testAdmin = dbContext.Admins.FirstOrDefault();
                    hasEmail = true;
                    hasName = true;
                }
                catch (Exception ex) when (ex.Message.Contains("Invalid column name 'Email'") || ex.Message.Contains("Invalid column name 'Name'"))
                {
                    logger.LogWarning("Email/Name kolonları henüz yok, sadece UserName ve Sifre ile ekleniyor...");
                }
                
                if (hasEmail && hasName)
                {
                    // Yeni yapı: Email ve Name ile
                var admin = new Kaya_Otel.Models.Admin 
                { 
                    Id = 1, 
                        Name = "Admin",
                        Email = "admin@kekovatur.com",
                        Sifre = "123",
                        CreatedAt = DateTime.UtcNow
                };
                dbContext.Admins.Add(admin);
                var saved = dbContext.SaveChanges();
                    logger.LogInformation($"✅ Admin kullanıcısı eklendi (Email: {admin.Email}). Kaydedilen kayıt sayısı: {saved}");
                
                // Kontrol et
                    var verifyAdmin = dbContext.Admins.FirstOrDefault(a => a.Email.ToLower() == "admin@kekovatur.com");
                if (verifyAdmin != null)
                {
                        logger.LogInformation($"✅ Admin doğrulandı: {verifyAdmin.Email} - {verifyAdmin.Name}");
                }
                else
                {
                    logger.LogWarning("⚠️ Admin eklenmiş görünüyor ama doğrulanamadı!");
                    }
                }
                else
                {
                    // Eski yapı: UserName ile (kolonlar henüz eklenmemiş)
                    dbContext.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM Admins WHERE Id = 1)
                        BEGIN
                            INSERT INTO Admins (Id, UserName, Sifre) 
                            VALUES (1, 'admin', '123');
                        END
                    ");
                    logger.LogInformation("✅ Admin kullanıcısı eklendi (eski yapı: UserName ile).");
                }
            }
            else
            {
                logger.LogInformation($"ℹ️ Admin kullanıcısı zaten mevcut ({adminCount} adet).");
                
                // Mevcut admin'lerin Email ve Name kolonlarını güncelle (yoksa)
                try
                {
                    dbContext.Database.ExecuteSqlRaw(@"
                        UPDATE Admins 
                        SET Email = ISNULL(Email, UserName + '@kekovatur.com'),
                            Name = ISNULL(Name, ISNULL(UserName, 'Admin')),
                            CreatedAt = ISNULL(CreatedAt, GETUTCDATE())
                        WHERE Email IS NULL OR Name IS NULL OR CreatedAt IS NULL;
                    ");
                    logger.LogInformation("✅ Mevcut admin kayıtları güncellendi (Email/Name/CreatedAt eklendi).");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Mevcut admin kayıtları güncellenirken hata: {ex.Message}");
                }
            }
            
            var tourCount = dbContext.Tours.Count();
            logger.LogInformation($"Mevcut tur sayısı: {tourCount}");
            
            if (tourCount == 0)
            {
                var tours = new List<Kaya_Otel.Models.Tour>
                {
                    new Kaya_Otel.Models.Tour
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
                    new Kaya_Otel.Models.Tour
                    {
                        Id = 2,
                        Name = "Mehtap Turu",
                        Category = "Mehtap",
                        Description = "Gece ışıkları altında Kekovanın sakin sularında mehtap turu.",
                        PricePerPerson = 12000,
                        Capacity = 12,
                        ImageUrl = "/images/günlükturfoto.jpg",
                        IsActive = true
                    },
                    new Kaya_Otel.Models.Tour
                    {
                        Id = 3,
                        Name = "Günlük Yemekli Özel Tekne Turu",
                        Category = "Tam Günlük",
                        Description = "Koy koy gezerek kekovanın tarihi doğasında akdeniz lezzetlerinin de sizlere eşlik ettiği tur keyfi.",
                        PricePerPerson = 20000,
                        Capacity = 12,
                        Duration = TimeSpan.FromHours(7),
                        ImageUrl = "/images/kekova1.jpg",
                        IsActive = true
                    }
                };
                
                dbContext.Tours.AddRange(tours);
                var saved = dbContext.SaveChanges();
                logger.LogInformation($"✅ Turlar eklendi. Kaydedilen kayıt sayısı: {saved}");
                
                // Kontrol et
                var verifyTours = dbContext.Tours.Count();
                logger.LogInformation($"✅ Turlar doğrulandı: {verifyTours} adet tur mevcut");
            }
            else
            {
                logger.LogInformation($"ℹ️ Turlar zaten mevcut ({tourCount} adet).");
            }
            
            // Kullanıcıları ve rezervasyonları ekle
            var userCount = await dbContext.Users.CountAsync();
            if (userCount == 0 || !await dbContext.Users.AnyAsync(u => u.Email == "irem1@hotmail.com"))
            {
                var random = new Random();
                var phoneNumbers = new[] { "05321234567", "05329876543", "05325554433", "05324443322", "05323332211", "05322221100" };
                
                var users = new List<Kaya_Otel.Models.user>();
                for (int i = 1; i <= 6; i++)
                {
                    var user = new Kaya_Otel.Models.user
                    {
                        Name = $"İrem {i}",
                        Email = $"irem{i}@hotmail.com",
                        Password = "123",
                        CreatedAt = DateTime.UtcNow.AddMonths(-(6 - i)) // Her kullanıcı farklı ayda kayıt olsun
                    };
                    users.Add(user);
                }
                
                dbContext.Users.AddRange(users);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("✅ 6 kullanıcı eklendi");
                
                // Turları al
                var tours = await dbContext.Tours.Where(t => t.IsActive).ToListAsync();
                if (tours.Any())
                {
                    var bookings = new List<Kaya_Otel.Models.Booking>();
                    var currentDate = DateTime.Now;
                    var currentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
                    
                    // Her kullanıcı için 2'şer rezervasyon yap
                    for (int userIndex = 0; userIndex < users.Count; userIndex++)
                    {
                        var user = users[userIndex];
                        var userPhone = phoneNumbers[userIndex];
                        
                        // Her kullanıcı için 2 rezervasyon
                        for (int bookingIndex = 0; bookingIndex < 2; bookingIndex++)
                        {
                            // Son 6 ay içinde rastgele bir ay seç
                            var monthOffset = userIndex + bookingIndex;
                            if (monthOffset >= 6) monthOffset = monthOffset % 6;
                            
                            var bookingMonth = currentMonth.AddMonths(-(5 - monthOffset));
                            var bookingDate = bookingMonth.AddDays(random.Next(1, 15)); // Ayın ilk 15 günü içinde
                            
                            // Rastgele bir tur seç
                            var tour = tours[random.Next(tours.Count)];
                            
                            var totalAmount = tour.PricePerPerson;
                            var depositAmount = Math.Round(totalAmount * 0.20m, 2); // %20 kapora
                            
                            var booking = new Kaya_Otel.Models.Booking
                            {
                                TourId = tour.Id,
                                UserId = user.Id,
                                TourName = tour.Name,
                                TourDate = bookingDate,
                                Guests = random.Next(1, 5), // 1-4 kişi arası
                                CustomerName = user.Name,
                                Email = user.Email,
                                Phone = userPhone,
                                TotalAmount = totalAmount,
                                DepositAmount = depositAmount,
                                IsDepositPaid = true, // Kapora ödendi
                                CreatedAt = bookingMonth.AddDays(random.Next(1, 10)) // Rezervasyon tarihi
                            };
                            
                            bookings.Add(booking);
                        }
                    }
                    
                    dbContext.Bookings.AddRange(bookings);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation($"✅ {bookings.Count} rezervasyon eklendi");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Seed data ekleme hatası: {Message}", ex.Message);
            logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Veritabanı oluşturma hatası: {Message}", ex.Message);
        logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();    // ← önce routing
app.UseSession();    // ← sonra session
app.UseAuthorization(); // ← sonra authorization


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
