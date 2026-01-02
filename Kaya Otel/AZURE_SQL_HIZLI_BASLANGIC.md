# Azure SQL'e Hızlı Başlangıç (Özet)

## 1. Azure Portal'da Veritabanı Oluştur
1. https://portal.azure.com → "Kaynak oluştur" → "SQL Database"
2. Sunucu oluştur (yeni oluştur):
   - Sunucu adı: `kayaotel-sql-server` (benzersiz bir isim)
   - Admin: `kayaoteladmin`
   - Parola: Güçlü bir parola (kaydedin!)
3. Veritabanı adı: `KayaOtelDb`
4. İşlem katmanı: "Sunucusuz" (düşük maliyet)
5. Ağ: "Azure hizmetlerine erişime izin ver" → **EVET**
6. "Oluştur" butonuna tıklayın

## 2. Connection String'i Al
1. Oluşturulan veritabanına tıklayın
2. "Bağlantı dizeleri" → "ADO.NET" sekmesi
3. Connection string'i kopyalayın
4. `{your_password}` kısmını gerçek parolanızla değiştirin

## 3. appsettings.json'ı Güncelle
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KayaOtelDb;...",
    "AzureConnection": "Server=tcp:kayaotel-sql-server.database.windows.net,1433;Initial Catalog=KayaOtelDb;User ID=kayaoteladmin;Password=GERÇEK_PAROLANIZ;Encrypt=True;..."
  }
}
```

## 4. Veritabanını Oluştur
Terminal'de:
```bash
dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

## 5. Test Et
Projeyi çalıştırın ve bağlantının çalıştığını kontrol edin.

---

**Not:** Program.cs dosyası otomatik olarak önce AzureConnection'ı kontrol eder, yoksa DefaultConnection'ı kullanır.

