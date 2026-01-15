-- DeletedTours tablosundaki tüm kayıtların DeletedAt saat bilgisini UTC'den Turkey Time'a (UTC+3) güncelle
-- Bu script'i SQL Server Management Studio'da veya veritabanı yönetim aracınızda çalıştırın

USE kekovatur;
GO

-- Tüm DeletedTours kayıtlarının DeletedAt saatlerini UTC'den Turkey Time'a (UTC+3) güncelle
-- Eğer saat zaten Turkey Time ise (yani 3 saat eklenmişse), tekrar eklememek için kontrol yapıyoruz
UPDATE [dbo].[DeletedTours]
SET [DeletedAt] = DATEADD(HOUR, 3, [DeletedAt])
WHERE [DeletedAt] < DATEADD(HOUR, 3, GETUTCDATE())
   OR [DeletedAt] < DATEADD(HOUR, -3, GETDATE()); -- Eğer saat UTC ise (GETDATE'den 3 saat gerideyse) güncelle

-- Güncelleme sonuçlarını kontrol et
SELECT 
    Id,
    Name,
    DeletedAt AS GuncelSaat,
    DATEADD(HOUR, -3, DeletedAt) AS ESkiSaat,
    DATEDIFF(HOUR, DATEADD(HOUR, -3, DeletedAt), DeletedAt) AS SaatFarki
FROM [dbo].[DeletedTours]
ORDER BY DeletedAt DESC;

PRINT '✅ DeletedTours tablosundaki tüm kayıtların saat bilgisi güncellendi (UTC -> Turkey Time).';
GO

