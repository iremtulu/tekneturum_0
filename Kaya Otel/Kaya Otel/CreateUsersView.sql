-- =============================================
-- USERS VIEW - ŞİFRELERİ MASKELENMİŞ GÖSTER
-- =============================================
-- Bu view Users tablosundaki şifreleri yıldızlı (*) olarak gösterir
-- =============================================

USE kekovatur_Dev;
GO

-- Eğer view zaten varsa sil
IF EXISTS (SELECT * FROM sys.views WHERE name = 'v_Users_Masked' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP VIEW dbo.v_Users_Masked;
    PRINT 'Mevcut view silindi.';
END
GO

-- View oluştur
CREATE VIEW dbo.v_Users_Masked
AS
SELECT 
    Id,
    Name,
    Email,
    REPLICATE('*', LEN(Password)) AS Password,  -- Şifreyi yıldızlı göster
    CreatedAt
FROM dbo.Users;
GO

PRINT '✅ v_Users_Masked view oluşturuldu.';
PRINT '';
PRINT 'Kullanım:';
PRINT 'SELECT * FROM v_Users_Masked;';
PRINT '';
PRINT 'Not: Orijinal Users tablosuna erişmek için:';
PRINT 'SELECT * FROM Users;';
GO

