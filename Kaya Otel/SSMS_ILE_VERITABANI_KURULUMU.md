# SQL Server Express + SSMS ile Veritabanı Kurulumu

## Adım 1: SSMS ile SQL Server Express'e Bağlanma

1. **SQL Server Management Studio (SSMS)**'yi açın
2. **"Connect to Server"** penceresi açılır:
   - **Server type**: Database Engine
   - **Server name**: `localhost\SQLEXPRESS` veya sadece `.\SQLEXPRESS`
   - **Authentication**: 
     - **Windows Authentication** (önerilen - Windows kullanıcınızla giriş)
     - veya **SQL Server Authentication** (sa kullanıcısı ve parolası ile)
   - **Connect** butonuna tıklayın

## Adım 2: Veritabanını Oluşturma

### Yöntem 1: SSMS ile Manuel Oluşturma

1. SSMS'de sol panelde **"Databases"** üzerine sağ tıklayın
2. **"New Database..."** seçin
3. **Database name**: `KayaOtelDb` yazın
4. **OK** butonuna tıklayın
5. Veritabanı oluşturuldu! ✅

### Yöntem 2: Migration ile Otomatik Oluşturma (Önerilen)

Terminal'de şu komutu çalıştırın:
```bash
dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

Bu komut:
- Tüm tabloları oluşturur
- İlişkileri (foreign keys) kurar
- Seed data'yı (Admin ve Turlar) ekler

## Adım 3: Connection String'i Güncelleme

`appsettings.json` dosyasını açın ve connection string'i güncelleyin:

### Windows Authentication ile:
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KayaOtelDb;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

### SQL Authentication ile (sa kullanıcısı):
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KayaOtelDb;User Id=sa;Password=parolanız;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

## Adım 4: Projeyi Test Etme

1. Projeyi çalıştırın
2. Veritabanı bağlantısının çalıştığını kontrol edin
3. SSMS'de veritabanını görüntüleyin:
   - Sol panelde **Databases** → **KayaOtelDb** → **Tables**
   - Tabloları görebilirsiniz: Tours, Bookings, Payments, Admins, Users, Rooms, Reservations

## Adım 5: SSMS ile Veri Kontrolü

### Tabloları Görüntüleme:
1. **KayaOtelDb** → **Tables** klasörünü genişletin
2. Herhangi bir tabloya sağ tıklayın
3. **"Select Top 1000 Rows"** seçin
4. Verileri görebilirsiniz

### Seed Data Kontrolü:
- **Admins** tablosunda: `admin` kullanıcısı olmalı
- **Tours** tablosunda: 3 tur olmalı (Günbatımı, Mehtap, Tam Günlük)

## Sorun Giderme

### Bağlantı Hatası Alıyorsanız:

1. **SQL Server'ın çalıştığından emin olun:**
   - Windows tuşu + R → `services.msc` yazın
   - **SQL Server (SQLEXPRESS)** servisinin **Running** olduğundan emin olun
   - Değilse, sağ tıklayıp **Start** seçin

2. **Server name'i kontrol edin:**
   - SSMS'de **View** → **Registered Servers**
   - Veya **Object Explorer**'da **Connect** → **Database Engine**
   - Server name'i doğru yazın: `localhost\SQLEXPRESS` veya `.\SQLEXPRESS`

3. **SQL Server Browser servisini başlatın:**
   - `services.msc` → **SQL Server Browser** → **Start**

### Migration Hatası Alıyorsanız:

1. **Veritabanı zaten varsa:**
   ```bash
   # Mevcut migration'ları kontrol et
   dotnet ef migrations list --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
   
   # Veritabanını silip yeniden oluştur (DİKKAT: Tüm veriler silinir!)
   # SSMS'de: KayaOtelDb → Sağ tık → Delete → OK
   # Sonra migration'ı tekrar çalıştır
   dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
   ```

## Hızlı Başlangıç Özeti

1. ✅ SSMS'yi aç → `localhost\SQLEXPRESS`'e bağlan
2. ✅ `appsettings.json`'da connection string'i güncelle
3. ✅ Terminal'de: `dotnet ef database update`
4. ✅ Projeyi çalıştır ve test et
5. ✅ SSMS'de veritabanını kontrol et

## SSMS ile Veritabanı Yönetimi

### Veri Ekleme/Düzenleme:
- Tabloya sağ tık → **Edit Top 200 Rows**
- Verileri doğrudan düzenleyebilirsiniz

### Sorgu Çalıştırma:
- **New Query** butonuna tıklayın
- SQL sorguları yazabilirsiniz:
  ```sql
  SELECT * FROM Tours;
  SELECT * FROM Admins;
  ```

### Backup/Restore:
- Veritabanına sağ tık → **Tasks** → **Back Up...**
- Yedek alabilir ve geri yükleyebilirsiniz

