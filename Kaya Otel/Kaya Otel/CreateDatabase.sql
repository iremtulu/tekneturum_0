-- Veritabanını oluştur (eğer yoksa)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'kekovatur_Dev')
BEGIN
    CREATE DATABASE kekovatur_Dev;
    PRINT 'Veritabanı oluşturuldu: kekovatur_Dev';
END
ELSE
BEGIN
    PRINT 'Veritabanı zaten mevcut: kekovatur_Dev';
END
GO

USE kekovatur_Dev;
GO

-- Admins tablosuna eksik kolonları ekle
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.Admins') AND type in (N'U'))
BEGIN
    -- Email kolonu
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
    
    -- Name kolonu
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
    
    -- CreatedAt kolonu
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Admins') AND name = 'CreatedAt')
    BEGIN
        ALTER TABLE dbo.Admins ADD CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();
        PRINT 'Admins.CreatedAt kolonu eklendi.';
    END
END
ELSE
BEGIN
    PRINT 'Admins tablosu bulunamadı. Uygulama ilk çalıştığında otomatik oluşturulacak.';
END
GO

PRINT 'Kolon ekleme işlemi tamamlandı.';
GO

