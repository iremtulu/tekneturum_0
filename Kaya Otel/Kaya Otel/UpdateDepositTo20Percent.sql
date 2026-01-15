-- Kapora ücretini %20'ye güncelleme script'i
-- Bu script, tüm rezervasyonların (aktif ve iptal edilmiş) kapora ücretlerini tur ücretinin %20'si olarak günceller

-- Aktif rezervasyonlar için kapora ücretini güncelle
UPDATE Bookings
SET DepositAmount = ROUND(TotalAmount * 0.20, 2)
WHERE DepositAmount != ROUND(TotalAmount * 0.20, 2);

-- İptal edilmiş rezervasyonlar için kapora ücretini güncelle
UPDATE CancelledBookings
SET DepositAmount = ROUND(TotalAmount * 0.20, 2)
WHERE DepositAmount != ROUND(TotalAmount * 0.20, 2);

-- Güncelleme sonuçlarını kontrol et
SELECT 
    'Aktif Rezervasyonlar' AS Tablo,
    COUNT(*) AS ToplamKayit,
    SUM(CASE WHEN DepositAmount = ROUND(TotalAmount * 0.20, 2) THEN 1 ELSE 0 END) AS GuncellenmisKayit,
    SUM(CASE WHEN DepositAmount != ROUND(TotalAmount * 0.20, 2) THEN 1 ELSE 0 END) AS GuncellenmemisKayit
FROM Bookings
UNION ALL
SELECT 
    'İptal Edilmiş Rezervasyonlar' AS Tablo,
    COUNT(*) AS ToplamKayit,
    SUM(CASE WHEN DepositAmount = ROUND(TotalAmount * 0.20, 2) THEN 1 ELSE 0 END) AS GuncellenmisKayit,
    SUM(CASE WHEN DepositAmount != ROUND(TotalAmount * 0.20, 2) THEN 1 ELSE 0 END) AS GuncellenmemisKayit
FROM CancelledBookings;

