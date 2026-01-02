# SSMS'de Veri Görüntüleme Rehberi

## Adım 1: Tablolardaki Verileri Görüntüleme

### Yöntem 1: Sağ Tık Menüsü (En Kolay)

1. SSMS'de `kekovatur` veritabanını genişletin
2. **Tables** klasörünü genişletin
3. Görüntülemek istediğiniz tabloya sağ tıklayın (örn: `Admins`)
4. **"Select Top 1000 Rows"** seçeneğine tıklayın
5. Veriler görünecektir! ✅

### Yöntem 2: SQL Sorgusu ile

1. SSMS'de üst menüden **"New Query"** butonuna tıklayın
2. Şu sorguları yazın:

**Admins tablosunu görüntüle:**
```sql
SELECT * FROM Admins;
```

**Tours tablosunu görüntüle:**
```sql
SELECT * FROM Tours;
```

**Bookings tablosunu görüntüle:**
```sql
SELECT * FROM Bookings;
```

**Payments tablosunu görüntüle:**
```sql
SELECT * FROM Payments;
```

3. **F5** tuşuna basın veya **Execute** butonuna tıklayın
4. Sonuçlar alt panelde görünecektir

## Adım 2: Veri Kontrolü

### Admin Kullanıcısı Kontrolü:
```sql
SELECT * FROM Admins;
```
**Beklenen sonuç:**
- Id: 1
- UserName: admin
- Sifre: 123

### Turlar Kontrolü:
```sql
SELECT * FROM Tours;
```
**Beklenen sonuç:** 3 tur
1. Özel Kekova Günbatımı Turu
2. Mehtap Turu
3. Günlük Yemekli Özel Tekne Turu

### Rezervasyonlar Kontrolü:
```sql
SELECT * FROM Bookings;
```
**Beklenen sonuç:** Rezervasyon yapan müşterilerin bilgileri

## Adım 3: Veri Yoksa Ne Yapmalı?

### Seed Data Yoksa:

Eğer `Admins` veya `Tours` tabloları boşsa, projeyi yeniden başlatın. Seed data otomatik olarak eklenir.

### Rezervasyon Verisi Yoksa:

Bu normal! Rezervasyon verileri sadece müşteri rezervasyon yaptığında eklenir. Test için:
1. Projeyi çalıştırın
2. Bir tur seçin
3. Rezervasyon yapın
4. SSMS'de `Bookings` tablosunu kontrol edin

## Adım 4: Veri Ekleme/Düzenleme (SSMS'den)

### Yeni Admin Ekleme:
```sql
INSERT INTO Admins (Id, UserName, Sifre)
VALUES (2, 'yeniadmin', 'yenisifre');
```

### Veri Güncelleme:
```sql
UPDATE Admins 
SET Sifre = 'yenisifre123' 
WHERE UserName = 'admin';
```

### Veri Silme:
```sql
DELETE FROM Bookings WHERE Id = 1;
```

## Sorun Giderme

### Tablolar Görünmüyorsa:
1. Veritabanını yenileyin (sağ tık → Refresh)
2. Tables klasörünü genişletin
3. Hala görünmüyorsa, projeyi yeniden çalıştırın

### Veriler Görünmüyorsa:
1. Tabloya sağ tık → "Select Top 1000 Rows"
2. Veya SQL sorgusu ile kontrol edin
3. Projeyi yeniden başlatın (seed data eklenir)

### Bağlantı Hatası:
1. Connection string'i kontrol edin
2. SQL Server'ın çalıştığından emin olun
3. Doğru instance adını kullandığınızdan emin olun

