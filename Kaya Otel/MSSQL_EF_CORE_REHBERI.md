# MSSQL + Entity Framework Core Rehberi

## ✅ Mevcut Durum

Projeniz şu anda **MSSQL (SQL Server LocalDB) + Entity Framework Core** kullanıyor ve çalışıyor!

## 📋 Yapılandırma

### 1. Connection String Seçenekleri

#### LocalDB (Geliştirme - Şu an kullanılan)
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KayaOtelDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

#### SQL Server Express (Production için)
```json
"SqlServerExpress": "Server=localhost\\SQLEXPRESS;Database=KayaOtelDb;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

#### SQL Server (Named Instance)
```json
"Server=localhost\\MSSQLSERVER;Database=KayaOtelDb;Integrated Security=True;TrustServerCertificate=True;"
```

#### SQL Server (SQL Authentication)
```json
"Server=localhost;Database=KayaOtelDb;User Id=sa;Password=parolanız;TrustServerCertificate=True;"
```

## 🔧 SQL Server Express Kurulumu (Production için)

### Adım 1: SQL Server Express İndirme
1. https://www.microsoft.com/sql-server/sql-server-downloads adresine gidin
2. **"Express"** versiyonunu indirin (ücretsiz)
3. Kurulum dosyasını çalıştırın

### Adım 2: Kurulum Ayarları
1. **Installation Type**: "Basic" veya "Custom" seçin
2. **Instance Configuration**: 
   - Instance Name: `SQLEXPRESS` (varsayılan)
   - Instance ID: `MSSQLSERVER` veya `SQLEXPRESS`
3. **Server Configuration**:
   - Service Account: `NT AUTHORITY\SYSTEM` (varsayılan)
4. **Database Engine Configuration**:
   - **Authentication Mode**: **"Mixed Mode"** seçin (SQL Authentication + Windows Authentication)
   - **SA Password**: Güçlü bir parola belirleyin (kaydedin!)
   - **Add Current User**: Tıklayın (Windows Authentication için)

### Adım 3: Kurulumu Tamamla
1. Kurulum tamamlanana kadar bekleyin
2. SQL Server Management Studio (SSMS) kurulumunu da seçebilirsiniz (veritabanını görsel olarak yönetmek için)

### Adım 4: Connection String'i Güncelle
`appsettings.json` dosyasında connection string'i güncelleyin:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KayaOtelDb;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Veya SQL Authentication kullanmak isterseniz:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=KayaOtelDb;User Id=sa;Password=parolanız;TrustServerCertificate=True;"
```

### Adım 5: Veritabanını Oluştur
Terminal'de:
```bash
dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

## 🎯 Entity Framework Core Özellikleri

### Migration Komutları

**Yeni migration oluştur:**
```bash
dotnet ef migrations add MigrationAdi --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

**Veritabanını güncelle:**
```bash
dotnet ef database update --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

**Migration'ları geri al:**
```bash
dotnet ef database update ÖncekiMigrationAdi --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

**Migration'ları sil:**
```bash
dotnet ef migrations remove --project "Kaya Otel\Kaya Otel\Kaya Otel.csproj"
```

### DbContext Kullanımı

**Controller'larda:**
```csharp
public class MyController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public MyController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        var tours = await _context.Tours.ToListAsync();
        return View(tours);
    }
}
```

## 🔒 Güvenlik Best Practices

1. **Connection String'i Güvenli Tutun:**
   - `appsettings.json` dosyasını `.gitignore`'a ekleyin
   - Production'da environment variables kullanın

2. **SQL Injection Koruması:**
   - Entity Framework Core otomatik olarak parametreli sorgular kullanır
   - Raw SQL kullanırken dikkatli olun

3. **Parola Yönetimi:**
   - Production'da güçlü parolalar kullanın
   - Parolaları asla kod içinde saklamayın

## 📊 Veritabanı Yönetimi

### SQL Server Management Studio (SSMS)
- Veritabanını görsel olarak yönetmek için
- İndirme: https://aka.ms/ssmsfullsetup

### Visual Studio'dan
- **View** → **SQL Server Object Explorer**
- LocalDB veya SQL Server Express'e bağlanabilirsiniz

## 🚀 Production Deployment

### Seçenek 1: SQL Server Express (Ücretsiz)
- 10 GB veritabanı limiti
- Aynı sunucuda çalıştırılabilir
- Ücretsiz

### Seçenek 2: SQL Server Standard/Enterprise
- Limit yok
- Daha yüksek performans
- Lisans gerekir

### Seçenek 3: SQL Server (Linux)
- Docker container olarak çalıştırılabilir
- Ücretsiz (Express)

## ⚙️ Yapılandırma İyileştirmeleri

Program.cs'de yapılan iyileştirmeler:
- ✅ Retry policy (bağlantı hatası durumunda 3 kez tekrar dener)
- ✅ Development ortamında detaylı hata mesajları
- ✅ Connection string validation

## 📝 Özet

✅ **MSSQL + EF Core** yapısı kurulu ve çalışıyor
✅ **LocalDB** geliştirme için kullanılıyor
✅ **SQL Server Express** production için hazır
✅ **Migration** sistemi çalışıyor
✅ **Seed data** (Admin ve Turlar) otomatik ekleniyor

## 🔍 Sorun Giderme

**Bağlantı hatası alıyorsanız:**
1. SQL Server'ın çalıştığından emin olun
2. Connection string'i kontrol edin
3. Firewall ayarlarını kontrol edin
4. SQL Server Authentication'ın açık olduğundan emin olun

**Migration hatası alıyorsanız:**
1. Mevcut migration'ları kontrol edin
2. Veritabanını silip yeniden oluşturun (geliştirme ortamında)
3. `dotnet ef migrations remove` ile son migration'ı silin

