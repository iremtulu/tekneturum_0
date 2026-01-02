# Local Veritabanı Seçenekleri (Azure Olmadan)

## 1. ✅ SQL Server LocalDB (Şu an kullanılan)
**Durum:** Zaten kurulu ve çalışıyor

**Avantajlar:**
- Visual Studio ile birlikte gelir
- Kurulum gerektirmez
- SQL Server'ın hafif versiyonu
- Ücretsiz

**Connection String:**
```
Server=(localdb)\mssqllocaldb;Database=KayaOtelDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**Kullanım:** Geliştirme için ideal, production için SQL Server Express gerekir

---

## 2. 🆕 PostgreSQL (Önerilen - Ücretsiz)
**Avantajlar:**
- Tamamen ücretsiz
- Güçlü ve profesyonel
- Production'a hazır
- Entity Framework Core ile çalışır

**Kurulum:**
1. PostgreSQL'i indir: https://www.postgresql.org/download/windows/
2. Kurulum sırasında parola belirleyin
3. Projeye paket ekleyin

**Connection String:**
```
Server=localhost;Port=5432;Database=KayaOtelDb;User Id=postgres;Password=parolanız;
```

**Kullanım:** Hem geliştirme hem production için uygun

---

## 3. 🆕 MySQL (Ücretsiz)
**Avantajlar:**
- Ücretsiz ve açık kaynak
- Yaygın kullanım
- Basit kurulum

**Kurulum:**
1. MySQL'i indir: https://dev.mysql.com/downloads/installer/
2. Kurulum sırasında root parolası belirleyin
3. Projeye paket ekleyin

**Connection String:**
```
Server=localhost;Port=3306;Database=KayaOtelDb;User Id=root;Password=parolanız;
```

---

## 4. ✅ SQLite (En Basit - Dosya Tabanlı)
**Avantajlar:**
- Kurulum gerektirmez
- Tek dosya veritabanı
- Çok hafif
- Ücretsiz

**Dezavantajlar:**
- Production için uygun değil (çoklu kullanıcı desteği zayıf)
- Eşzamanlı yazma sınırlamaları

**Connection String:**
```
Data Source=KayaOtelDb.db
```

**Kullanım:** Sadece geliştirme ve test için

---

## 5. SQL Server Express (Ücretsiz - Production için)
**Avantajlar:**
- SQL Server'ın ücretsiz versiyonu
- Production'a uygun
- 10 GB veritabanı limiti (çoğu proje için yeterli)

**Kurulum:**
1. SQL Server Express'i indir: https://www.microsoft.com/sql-server/sql-server-downloads
2. Kurulum sırasında "Mixed Mode Authentication" seçin
3. SA parolası belirleyin

**Connection String:**
```
Server=localhost\SQLEXPRESS;Database=KayaOtelDb;User Id=sa;Password=parolanız;
```

**Kullanım:** Production için ideal

---

## Öneri: PostgreSQL veya SQL Server Express

### PostgreSQL Seçerseniz:
- ✅ Tamamen ücretsiz
- ✅ Production'a hazır
- ✅ Güçlü özellikler
- ✅ Entity Framework Core ile çalışır

### SQL Server Express Seçerseniz:
- ✅ Mevcut kod değişikliği minimal
- ✅ Production'a uygun
- ✅ 10 GB limit (çoğu proje için yeterli)

---

## Hızlı Karşılaştırma

| Veritabanı | Kurulum | Production | Limit | Öğrenme |
|------------|---------|------------|-------|---------|
| **LocalDB** | ✅ Hazır | ❌ | - | ⭐⭐⭐ |
| **PostgreSQL** | ⚠️ Kurulum | ✅ | Yok | ⭐⭐⭐ |
| **MySQL** | ⚠️ Kurulum | ✅ | Yok | ⭐⭐ |
| **SQLite** | ✅ Hazır | ❌ | - | ⭐ |
| **SQL Express** | ⚠️ Kurulum | ✅ | 10 GB | ⭐⭐⭐ |

---

## Şu Anki Durum

Projeniz şu anda **SQL Server LocalDB** kullanıyor ve çalışıyor. 

**Seçenekleriniz:**
1. **LocalDB'de kal** (geliştirme için yeterli)
2. **PostgreSQL'e geç** (ücretsiz, production'a hazır)
3. **SQL Server Express'e geç** (production için, mevcut kod değişikliği minimal)

Hangisini tercih edersiniz?

