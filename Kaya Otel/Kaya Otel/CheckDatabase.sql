-- =============================================
-- VERİTABANI KONTROL KOMUTLARI
-- =============================================
-- Bu script ile tüm tabloları ve verileri kontrol edebilirsiniz
-- =============================================

USE kekovatur_Dev;
GO

-- =============================================
-- 1. ADMINS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== ADMINS TABLOSU ===';
SELECT 
    Id,
    Name,
    Email,
    UserName,
    REPLICATE('*', LEN(Sifre)) AS Sifre,
    CreatedAt
FROM Admins;
GO

-- Admin sayısı
SELECT COUNT(*) AS AdminSayisi FROM Admins;
GO

-- Belirli bir admin kontrolü (şifre maskelenmiş)
-- SELECT Id, Name, Email, UserName, REPLICATE('*', LEN(Sifre)) AS Sifre, CreatedAt FROM Admins WHERE Email = 'admin@kekovatur.com';
-- SELECT Id, Name, Email, UserName, REPLICATE('*', LEN(Sifre)) AS Sifre, CreatedAt FROM Admins WHERE Email = 'test@example.com';
GO

-- =============================================
-- 2. USERS TABLOSU KONTROLÜ (Şifreler Maskelenmiş)
-- =============================================
PRINT '=== USERS TABLOSU (Şifreler Maskelenmiş) ===';

-- View varsa kullan, yoksa direkt sorgu çalıştır
IF EXISTS (SELECT * FROM sys.views WHERE name = 'v_Users_Masked' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    SELECT * FROM v_Users_Masked;
    PRINT 'Not: v_Users_Masked view kullanıldı. Şifreler yıldızlı gösteriliyor.';
END
ELSE
BEGIN
    SELECT 
        Id,
        Name,
        Email,
        REPLICATE('*', LEN(Password)) AS Password,
        CreatedAt
    FROM Users;
    PRINT 'Not: View bulunamadı, direkt sorgu kullanıldı. Şifreler yıldızlı gösteriliyor.';
    PRINT 'View oluşturmak için: CreateUsersView.sql dosyasını çalıştırın.';
END
GO

-- Kullanıcı sayısı
SELECT COUNT(*) AS KullaniciSayisi FROM Users;
GO

-- Belirli bir kullanıcı kontrolü (şifre maskelenmiş)
-- SELECT Id, Name, Email, REPLICATE('*', LEN(Password)) AS Password, CreatedAt FROM Users WHERE Email = 'test@example.com';
GO

-- =============================================
-- 3. TOURS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== TOURS TABLOSU ===';
SELECT 
    Id,
    Name,
    Category,
    Description,
    PricePerPerson,
    Capacity,
    Duration,
    ImageUrl,
    IsActive
FROM Tours;
GO

-- Tur sayısı
SELECT COUNT(*) AS TurSayisi FROM Tours;
GO

-- =============================================
-- 4. DELETED TOURS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== DELETED TOURS TABLOSU ===';
SELECT 
    Id,
    OriginalTourId,
    Name,
    Category,
    DeletedAt,
    DeletedBy
FROM DeletedTours;
GO

-- Silinen tur sayısı
SELECT COUNT(*) AS SilinenTurSayisi FROM DeletedTours;
GO

-- =============================================
-- 5. BOOKINGS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== BOOKINGS TABLOSU ===';
SELECT 
    Id,
    TourId,
    UserId,
    TourName,
    TourDate,
    Guests,
    CustomerName,
    Email,
    Phone,
    TotalAmount,
    DepositAmount,
    IsDepositPaid,
    CreatedAt
FROM Bookings;
GO

-- Rezervasyon sayısı
SELECT COUNT(*) AS RezervasyonSayisi FROM Bookings;
GO

-- Kullanıcıya göre rezervasyonlar
-- SELECT * FROM Bookings WHERE UserId = 1;
-- SELECT * FROM Bookings WHERE Email = 'test@example.com';
GO

-- =============================================
-- 6. CANCELLED BOOKINGS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== CANCELLED BOOKINGS TABLOSU ===';
SELECT 
    Id,
    OriginalBookingId,
    TourId,
    TourName,
    CustomerName,
    Email,
    CancelledAt,
    CancelledBy,
    CancellationReason
FROM CancelledBookings;
GO

-- İptal edilen rezervasyon sayısı
SELECT COUNT(*) AS IptalEdilenRezervasyonSayisi FROM CancelledBookings;
GO

-- =============================================
-- 7. PAYMENTS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== PAYMENTS TABLOSU ===';
SELECT 
    Id,
    BookingId,
    Amount,
    Provider,
    Status,
    TransactionId,
    PaidAt
FROM Payments;
GO

-- Ödeme sayısı
SELECT COUNT(*) AS OdemeSayisi FROM Payments;
GO

-- =============================================
-- 8. ROOMS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== ROOMS TABLOSU ===';
SELECT 
    Id,
    Name,
    Price,
    MaxAdults,
    MaxChildren,
    Capacity,
    Available,
    CheckIn,
    CheckOut
FROM Rooms;
GO

-- Oda sayısı
SELECT COUNT(*) AS OdaSayisi FROM Rooms;
GO

-- =============================================
-- 9. RESERVATIONS TABLOSU KONTROLÜ
-- =============================================
PRINT '=== RESERVATIONS TABLOSU ===';
SELECT 
    Id,
    RoomId,
    RoomName,
    Available,
    Price,
    CheckIn,
    CheckOut,
    Adults,
    Children,
    TotalPrice,
    IsPaid
FROM Reservations;
GO

-- Rezervasyon sayısı
SELECT COUNT(*) AS OdaRezervasyonSayisi FROM Reservations;
GO

-- =============================================
-- 10. TÜM TABLOLARIN ÖZET BİLGİSİ
-- =============================================
PRINT '=== TABLO ÖZET BİLGİLERİ ===';
SELECT 
    'Admins' AS TabloAdi, COUNT(*) AS KayitSayisi FROM Admins
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Tours', COUNT(*) FROM Tours
UNION ALL
SELECT 'DeletedTours', COUNT(*) FROM DeletedTours
UNION ALL
SELECT 'Bookings', COUNT(*) FROM Bookings
UNION ALL
SELECT 'CancelledBookings', COUNT(*) FROM CancelledBookings
UNION ALL
SELECT 'Payments', COUNT(*) FROM Payments
UNION ALL
SELECT 'Rooms', COUNT(*) FROM Rooms
UNION ALL
SELECT 'Reservations', COUNT(*) FROM Reservations;
GO

-- =============================================
-- 11. KOLON YAPILARINI KONTROL ET
-- =============================================
PRINT '=== ADMINS TABLOSU KOLONLARI ===';
SELECT 
    COLUMN_NAME AS KolonAdi,
    DATA_TYPE AS VeriTipi,
    IS_NULLABLE AS NullOlabilir,
    CHARACTER_MAXIMUM_LENGTH AS MaksimumUzunluk
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Admins'
ORDER BY ORDINAL_POSITION;
GO

PRINT '=== USERS TABLOSU KOLONLARI ===';
SELECT 
    COLUMN_NAME AS KolonAdi,
    DATA_TYPE AS VeriTipi,
    IS_NULLABLE AS NullOlabilir,
    CHARACTER_MAXIMUM_LENGTH AS MaksimumUzunluk
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;
GO

-- =============================================
-- 12. BELİRLİ BİR KULLANICIYI KONTROL ET (ŞİFRELER MASKELENMİŞ)
-- =============================================
-- Aşağıdaki komutları kendi email'inizle değiştirerek kullanabilirsiniz
-- SELECT Id, Name, Email, REPLICATE('*', LEN(Password)) AS Password, CreatedAt FROM Users WHERE Email = 'test@example.com';
-- SELECT Id, Name, Email, UserName, REPLICATE('*', LEN(Sifre)) AS Sifre, CreatedAt FROM Admins WHERE Email = 'admin@kekovatur.com';

-- =============================================
-- 13. KULLANICI VE REZERVASYON İLİŞKİSİ
-- =============================================
PRINT '=== KULLANICI REZERVASYONLARI ===';
SELECT 
    u.Id AS UserId,
    u.Name AS KullaniciAdi,
    u.Email AS KullaniciEmail,
    b.Id AS BookingId,
    b.TourName,
    b.TourDate,
    b.Guests,
    b.TotalAmount
FROM Users u
LEFT JOIN Bookings b ON u.Id = b.UserId
ORDER BY u.Id, b.CreatedAt DESC;
GO

-- =============================================
-- 14. SON EKLENEN KAYITLAR
-- =============================================
PRINT '=== SON EKLENEN KULLANICILAR ===';
SELECT TOP 10 
    Id,
    Name,
    Email,
    CreatedAt
FROM Users
ORDER BY CreatedAt DESC;
GO

PRINT '=== SON EKLENEN ADMİNLER ===';
SELECT TOP 10 
    Id,
    Name,
    Email,
    CreatedAt
FROM Admins
ORDER BY CreatedAt DESC;
GO

PRINT '';
PRINT '=============================================';
PRINT '✅ KONTROL TAMAMLANDI!';
PRINT '=============================================';

