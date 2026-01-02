using Kaya_Otel.Data;
using Kaya_Otel.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Database yapılandırması - MSSQL + EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' bulunamadı.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
    
    // Development ortamında detaylı hata mesajları
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});


builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IIyzicoPaymentService, IyzicoPaymentService>();
// Instagram servisini yapılandır - token varsa gerçek API, yoksa fake servis kullan
var instagramToken = builder.Configuration["Instagram:AccessToken"];
if (!string.IsNullOrEmpty(instagramToken))
{
    builder.Services.AddScoped<IInstagramService, InstagramGraphService>();
}
else
{
    builder.Services.AddScoped<IInstagramService, FakeInstagramService>();
}
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
  options.IdleTimeout = TimeSpan.FromMinutes(30);
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Veritabanını otomatik oluştur ve seed data ekle (ilk çalıştırmada)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Veritabanı bağlantısını test et
        var canConnect = dbContext.Database.CanConnect();
        logger.LogInformation($"Veritabanı bağlantısı: {(canConnect ? "Başarılı" : "Başarısız")}");
        
        // Tabloların var olup olmadığını kontrol et
        bool tablesExist = false;
        try
        {
            // Tours tablosuna basit bir sorgu yapmayı dene
            var test = dbContext.Tours.Count();
            tablesExist = true;
            logger.LogInformation("Tablolar mevcut.");
        }
        catch
        {
            logger.LogWarning("Tablolar bulunamadı. Oluşturuluyor...");
            tablesExist = false;
        }
        
        // Tablolar yoksa oluştur
        if (!tablesExist)
        {
            var created = dbContext.Database.EnsureCreated();
            if (created)
            {
                logger.LogInformation("✅ Veritabanı ve tablolar oluşturuldu.");
            }
            else
            {
                logger.LogWarning("⚠️ Tablolar oluşturulamadı. Veritabanı silinip yeniden oluşturuluyor...");
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                logger.LogInformation("✅ Veritabanı ve tablolar yeniden oluşturuldu.");
            }
        }
        
        // Seed data ekle (eğer yoksa)
        try
        {
            var adminCount = dbContext.Admins.Count();
            logger.LogInformation($"Mevcut admin sayısı: {adminCount}");
            
            if (adminCount == 0)
            {
                var admin = new Kaya_Otel.Models.Admin 
                { 
                    Id = 1, 
                    UserName = "admin", 
                    Sifre = "123" 
                };
                dbContext.Admins.Add(admin);
                var saved = dbContext.SaveChanges();
                logger.LogInformation($"✅ Admin kullanıcısı eklendi. Kaydedilen kayıt sayısı: {saved}");
                
                // Kontrol et
                var verifyAdmin = dbContext.Admins.FirstOrDefault(a => a.UserName == "admin");
                if (verifyAdmin != null)
                {
                    logger.LogInformation($"✅ Admin doğrulandı: {verifyAdmin.UserName}");
                }
                else
                {
                    logger.LogWarning("⚠️ Admin eklenmiş görünüyor ama doğrulanamadı!");
                }
            }
            else
            {
                logger.LogInformation($"ℹ️ Admin kullanıcısı zaten mevcut ({adminCount} adet).");
            }
            
            var tourCount = dbContext.Tours.Count();
            logger.LogInformation($"Mevcut tur sayısı: {tourCount}");
            
            if (tourCount == 0)
            {
                var tours = new List<Kaya_Otel.Models.Tour>
                {
                    new Kaya_Otel.Models.Tour
                    {
                        Id = 1,
                        Name = "Özel Kekova Günbatımı Turu",
                        Category = "Günbatımı",
                        Description = "Gün batımını Kekovanın koylarında karşılayan tekne turu.",
                        PricePerPerson = 15000,
                        Capacity = 12,
                        ImageUrl = "/images/sunset.jpg",
                        IsActive = true
                    },
                    new Kaya_Otel.Models.Tour
                    {
                        Id = 2,
                        Name = "Mehtap Turu",
                        Category = "Mehtap",
                        Description = "Gece ışıkları altında Kekovanın sakin sularında mehtap turu.",
                        PricePerPerson = 12000,
                        Capacity = 12,
                        ImageUrl = "/images/günlükturfoto.jpg",
                        IsActive = true
                    },
                    new Kaya_Otel.Models.Tour
                    {
                        Id = 3,
                        Name = "Günlük Yemekli Özel Tekne Turu",
                        Category = "Tam Günlük",
                        Description = "Koy koy gezerek kekovanın tarihi doğasında akdeniz lezzetlerinin de sizlere eşlik ettiği tur keyfi.",
                        PricePerPerson = 20000,
                        Capacity = 12,
                        Duration = TimeSpan.FromHours(7),
                        ImageUrl = "/images/kekova1.jpg",
                        IsActive = true
                    }
                };
                
                dbContext.Tours.AddRange(tours);
                var saved = dbContext.SaveChanges();
                logger.LogInformation($"✅ Turlar eklendi. Kaydedilen kayıt sayısı: {saved}");
                
                // Kontrol et
                var verifyTours = dbContext.Tours.Count();
                logger.LogInformation($"✅ Turlar doğrulandı: {verifyTours} adet tur mevcut");
            }
            else
            {
                logger.LogInformation($"ℹ️ Turlar zaten mevcut ({tourCount} adet).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Seed data ekleme hatası: {Message}", ex.Message);
            logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Veritabanı oluşturma hatası: {Message}", ex.Message);
        logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();    // ← önce routing
app.UseSession();    // ← sonra session
app.UseAuthorization(); // ← sonra authorization


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
