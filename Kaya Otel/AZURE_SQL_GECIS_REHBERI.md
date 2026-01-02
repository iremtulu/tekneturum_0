# Azure SQL'e Geçiş Rehberi

## Adım 1: Azure SQL Veritabanı Oluşturma

### 1.1 Azure Portal'a Giriş
1. https://portal.azure.com adresine gidin
2. Azure hesabınızla giriş yapın (yoksa ücretsiz hesap oluşturun)

### 1.2 SQL Veritabanı Oluşturma
1. Azure Portal'da sol üstten **"Kaynak oluştur"** (Create a resource) butonuna tıklayın
2. Arama kutusuna **"SQL Database"** yazın ve seçin
3. **"Oluştur"** (Create) butonuna tıklayın

### 1.3 Veritabanı Ayarları
**Temel Bilgiler (Basics):**
- **Abonelik**: Mevcut aboneliğinizi seçin
- **Kaynak Grubu**: Yeni bir kaynak grubu oluşturun (örn: "KayaOtel-RG") veya mevcut birini seçin
- **Veritabanı adı**: `KayaOtelDb` (veya istediğiniz bir isim)
- **Sunucu**: "Yeni oluştur" (Create new) seçeneğine tıklayın
  - **Sunucu adı**: `kayaotel-sql-server` (benzersiz bir isim, küçük harf ve tire kullanın)
  - **Konum**: Türkiye için "West Europe" veya "East Europe" seçin
  - **Sunucu admin oturum açma adı**: `kayaoteladmin` (veya istediğiniz kullanıcı adı)
  - **Parola**: Güçlü bir parola oluşturun (kaydedin!)
  - **Parolayı onayla**: Aynı parolayı tekrar girin
- **İşlem + depolama**: 
  - **İşlem katmanı**: "Sunucusuz" (Serverless) - düşük maliyet için
  - **Min vCore**: 0.5
  - **Max vCore**: 2
  - **Otomatik duraklatma gecikmesi**: 60 dakika

**Ağ (Networking):**
- **Ağ bağlantısı**: 
  - **Bağlantı yöntemi**: "Genel uç nokta" (Public endpoint) seçin
  - **Azure hizmetlerine erişime izin ver**: **EVET** (Allow Azure services and resources to access this server)
  - **Mevcut istemci IP adresini ekle**: **EVET** (Add current client IP address)
  - **Güvenlik duvarı kuralları**: İsteğe bağlı olarak ek IP adresleri ekleyebilirsiniz

**Güvenlik (Security):**
- **Microsoft Defender for SQL**: Şimdilik "Şimdilik atla" (Skip for now) seçebilirsiniz

**Ek Ayarlar (Additional settings):**
- **Veri kaynağı**: "Boş veritabanı" (Blank database)
- **Veritabanı harmanlaması**: Varsayılan (SQL_Latin1_General_CP1_CI_AS)

4. **"Gözden geçir + oluştur"** (Review + create) butonuna tıklayın
5. Doğrulama başarılı olduktan sonra **"Oluştur"** (Create) butonuna tıklayın
6. Dağıtım tamamlanana kadar bekleyin (2-3 dakika)

## Adım 2: Connection String'i Bulma

### 2.1 Azure Portal'da Veritabanını Bulma
1. Azure Portal'da sol menüden **"SQL veritabanları"** (SQL databases) seçin
2. Oluşturduğunuz `KayaOtelDb` veritabanına tıklayın

### 2.2 Connection String'i Kopyalama
1. Sol menüden **"Bağlantı dizeleri"** (Connection strings) seçin
2. **"ADO.NET"** sekmesine tıklayın
3. Connection string'i kopyalayın (şu formatta olacak):
   ```
   Server=tcp:kayaotel-sql-server.database.windows.net,1433;Initial Catalog=KayaOtelDb;Persist Security Info=False;User ID=kayaoteladmin;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
4. `{your_password}` kısmını oluşturduğunuz gerçek parola ile değiştirin

## Adım 3: Projede Connection String'i Yapılandırma

### 3.1 appsettings.json Dosyasını Güncelleme
`appsettings.json` dosyasını açın ve `AzureConnection` kısmını doldurun:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KayaOtelDb;Trusted_Connection=True;MultipleActiveResultSets=true",
    "AzureConnection": "Server=tcp:kayaotel-sql-server.database.windows.net,1433;Initial Catalog=KayaOtelDb;Persist Security Info=False;User ID=kayaoteladmin;Password=GERÇEK_PAROLANIZ;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### 3.2 Program.cs Dosyasını Güncelleme
`Program.cs` dosyasında connection string seçimini güncelleyin (zaten yapıldı, kontrol edin).

## Adım 4: Veritabanını Azure SQL'e Migrate Etme

### 4.1 Migration'ı Azure SQL'e Uygulama
Terminal'de şu komutu çalıştırın:

```bash
dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj" --connection "AzureConnectionStringBuraya"
```

Veya daha kolay yöntem: `appsettings.json`'da `DefaultConnection`'ı geçici olarak `AzureConnection` ile değiştirin ve:

```bash
dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

## Adım 5: Güvenlik Duvarı Ayarları

Eğer bağlantı hatası alırsanız:

1. Azure Portal'da SQL sunucunuza gidin
2. **"Ağ"** (Networking) sekmesine tıklayın
3. **"Genel ağ erişimi"** (Public network access) bölümünde:
   - **"Seçili ağlar"** (Selected networks) seçin
   - **"Mevcut istemci IP adresini ekle"** (Add current client IP address) butonuna tıklayın
   - Veya **"Tüm Azure hizmetlerine izin ver"** (Allow Azure services and resources) seçeneğini açın

## Adım 6: Test Etme

1. Projeyi çalıştırın
2. Veritabanı bağlantısının çalıştığını kontrol edin
3. Admin paneline giriş yapın ve verilerin göründüğünü kontrol edin

## Önemli Notlar

⚠️ **Güvenlik:**
- Connection string'i asla GitHub'a yüklemeyin
- `appsettings.json` dosyasını `.gitignore`'a ekleyin
- Production'da Azure Key Vault kullanın

💰 **Maliyet:**
- Sunucusuz (Serverless) katman düşük maliyetlidir
- Kullanılmadığında otomatik duraklar
- İlk 32 GB depolama ücretsizdir

🔧 **Sorun Giderme:**
- Bağlantı hatası alırsanız güvenlik duvarı ayarlarını kontrol edin
- Connection string'de parolanın doğru olduğundan emin olun
- Timeout hatası alırsanız connection timeout değerini artırın

