-- =============================================
-- KEKOVA TOUR - TÜM TABLOLARI YENİDEN OLUŞTUR
-- =============================================
-- Bu script tüm tabloları, foreign key'leri, index'leri ve seed data'yı oluşturur
-- =============================================

USE master;
GO

-- Veritabanını oluştur (eğer yoksa)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'kekovatur_Dev')
BEGIN
    CREATE DATABASE kekovatur_Dev;
    PRINT '✅ Veritabanı oluşturuldu: kekovatur_Dev';
END
ELSE
BEGIN
    PRINT 'ℹ️ Veritabanı zaten mevcut: kekovatur_Dev';
END
GO

USE kekovatur_Dev;
GO

-- =============================================
-- MEVCUT TABLOLARI SİL (EĞER VARSA)
-- =============================================
-- Foreign key'ler nedeniyle sırayla silinmeli

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Payments') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Payments;
    PRINT 'Payments tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.CancelledBookings') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.CancelledBookings;
    PRINT 'CancelledBookings tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Bookings') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Bookings;
    PRINT 'Bookings tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.DeletedTours') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.DeletedTours;
    PRINT 'DeletedTours tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Tours') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Tours;
    PRINT 'Tours tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Reservations') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Reservations;
    PRINT 'Reservations tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Rooms') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Rooms;
    PRINT 'Rooms tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Users') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Users;
    PRINT 'Users tablosu silindi.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Admins') AND type in (N'U'))
BEGIN
    DROP TABLE dbo.Admins;
    PRINT 'Admins tablosu silindi.';
END

GO

-- =============================================
-- TABLOLARI OLUŞTUR
-- =============================================

-- 1. Admins Tablosu
CREATE TABLE [dbo].[Admins] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [UserName] nvarchar(100) NULL,
    [Sifre] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Admins] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [IX_Admins_Email] ON [dbo].[Admins]([Email]);
PRINT '✅ Admins tablosu oluşturuldu.';

-- 2. Users Tablosu
CREATE TABLE [dbo].[Users] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [Password] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
PRINT '✅ Users tablosu oluşturuldu.';

-- 3. Tours Tablosu
CREATE TABLE [dbo].[Tours] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NULL,
    [Description] nvarchar(1000) NULL,
    [PricePerPerson] decimal(18,2) NOT NULL,
    [Capacity] int NOT NULL,
    [Duration] time NOT NULL,
    [ImageUrl] nvarchar(500) NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Tours] PRIMARY KEY ([Id])
);
PRINT '✅ Tours tablosu oluşturuldu.';

-- 4. DeletedTours Tablosu
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
    [DeletedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [DeletedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_DeletedTours] PRIMARY KEY ([Id])
);
PRINT '✅ DeletedTours tablosu oluşturuldu.';

-- 5. Bookings Tablosu
CREATE TABLE [dbo].[Bookings] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [TourId] int NOT NULL,
    [UserId] int NULL,
    [TourName] nvarchar(200) NOT NULL,
    [TourDate] datetime2 NOT NULL,
    [Guests] int NOT NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [Email] nvarchar(200) NULL,
    [Phone] nvarchar(50) NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [IsDepositPaid] bit NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookings_Tours_TourId] FOREIGN KEY ([TourId]) REFERENCES [dbo].[Tours]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
);
PRINT '✅ Bookings tablosu oluşturuldu.';

-- 6. CancelledBookings Tablosu
CREATE TABLE [dbo].[CancelledBookings] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [OriginalBookingId] int NOT NULL,
    [TourId] int NOT NULL,
    [TourName] nvarchar(200) NOT NULL,
    [TourDate] datetime2 NOT NULL,
    [Guests] int NOT NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [Email] nvarchar(200) NULL,
    [Phone] nvarchar(50) NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [IsDepositPaid] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CancelledAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [CancelledBy] nvarchar(100) NULL,
    [CancellationReason] nvarchar(500) NULL,
    CONSTRAINT [PK_CancelledBookings] PRIMARY KEY ([Id])
);
PRINT '✅ CancelledBookings tablosu oluşturuldu.';

-- 7. Payments Tablosu
CREATE TABLE [dbo].[Payments] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [BookingId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Provider] nvarchar(50) NULL DEFAULT 'iyzico',
    [Status] nvarchar(50) NULL DEFAULT 'Pending',
    [TransactionId] nvarchar(200) NULL,
    [PaidAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [dbo].[Bookings]([Id]) ON DELETE NO ACTION
);
PRINT '✅ Payments tablosu oluşturuldu.';

-- 8. Rooms Tablosu
CREATE TABLE [dbo].[Rooms] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [MaxAdults] int NOT NULL DEFAULT 2,
    [MaxChildren] int NOT NULL DEFAULT 0,
    [Capacity] int NOT NULL DEFAULT 2,
    [Available] bit NOT NULL DEFAULT 1,
    [CheckIn] datetime2 NULL,
    [CheckOut] datetime2 NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
);
PRINT '✅ Rooms tablosu oluşturuldu.';

-- 9. Reservations Tablosu
CREATE TABLE [dbo].[Reservations] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [RoomId] int NOT NULL,
    [RoomName] nvarchar(200) NULL,
    [Available] bit NOT NULL DEFAULT 1,
    [Price] decimal(18,2) NOT NULL,
    [CheckIn] datetime2 NOT NULL,
    [CheckOut] datetime2 NOT NULL,
    [Adults] int NOT NULL DEFAULT 2,
    [Children] int NOT NULL DEFAULT 0,
    [TotalPrice] decimal(18,2) NOT NULL,
    [IsPaid] bit NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id])
);
PRINT '✅ Reservations tablosu oluşturuldu.';

GO

-- =============================================
-- SEED DATA EKLE
-- =============================================

-- Admin Seed Data
SET IDENTITY_INSERT [dbo].[Admins] ON;
INSERT INTO [dbo].[Admins] ([Id], [Name], [Email], [UserName], [Sifre], [CreatedAt])
VALUES (1, N'Admin', N'admin@kekovatur.com', N'admin', N'123', GETUTCDATE());
SET IDENTITY_INSERT [dbo].[Admins] OFF;
PRINT '✅ Admin seed data eklendi (Email: admin@kekovatur.com, Şifre: 123)';

-- Tour Seed Data
SET IDENTITY_INSERT [dbo].[Tours] ON;
INSERT INTO [dbo].[Tours] ([Id], [Name], [Category], [Description], [PricePerPerson], [Capacity], [Duration], [ImageUrl], [IsActive])
VALUES 
    (1, N'Özel Kekova Günbatımı Turu', N'Günbatımı', N'Gün batımını Kekovanın koylarında karşılayan tekne turu.', 15000.00, 12, '16:00:00', N'/images/sunset.jpg', 1),
    (2, N'Mehtap Turu', N'Mehtap', N'Gece ışıkları altında Kekovanın sakin sularında mehtap turu.', 12000.00, 12, '16:00:00', N'/images/kekova1.jpg', 1),
    (3, N'Günlük Yemekli Özel Tekne Turu', N'Tam Günlük', N'Koy koy gezerek kekovanın tarihi doğasında akdeniz lezzetlerinin de sizlere eşlik ettiği tur keyfi.', 20000.00, 12, '07:00:00', N'/images/günlükturfoto.jpg', 1);
SET IDENTITY_INSERT [dbo].[Tours] OFF;
PRINT '✅ Tour seed data eklendi (3 tur)';

GO

PRINT '';
PRINT '=============================================';
PRINT '✅ TÜM TABLOLAR BAŞARIYLA OLUŞTURULDU!';
PRINT '=============================================';
PRINT 'Admin Giriş Bilgileri:';
PRINT '  Email: admin@kekovatur.com';
PRINT '  Şifre: 123';
PRINT '=============================================';

