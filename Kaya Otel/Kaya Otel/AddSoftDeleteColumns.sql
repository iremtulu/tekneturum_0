-- Tour tablosuna IsDeleted ve DeletedAt kolonlarını ekle
-- Bu script'i SQL Server Management Studio'da veya veritabanı yönetim aracınızda çalıştırın

USE kekovatur;
GO

-- IsDeleted kolonunu ekle (varsa hata vermez)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Tours') AND name = 'IsDeleted')
BEGIN
    ALTER TABLE dbo.Tours
    ADD IsDeleted BIT NOT NULL DEFAULT 0;
    PRINT 'IsDeleted kolonu eklendi.';
END
ELSE
BEGIN
    PRINT 'IsDeleted kolonu zaten mevcut.';
END
GO

-- DeletedAt kolonunu ekle (varsa hata vermez)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Tours') AND name = 'DeletedAt')
BEGIN
    ALTER TABLE dbo.Tours
    ADD DeletedAt DATETIME2 NULL;
    PRINT 'DeletedAt kolonu eklendi.';
END
ELSE
BEGIN
    PRINT 'DeletedAt kolonu zaten mevcut.';
END
GO

PRINT 'Tüm kolonlar başarıyla eklendi!';
GO

