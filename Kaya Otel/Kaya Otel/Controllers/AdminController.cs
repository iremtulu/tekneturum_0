using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Kaya_Otel.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private bool HasAdminAccess => HttpContext.Session.GetString("AdminGirisi") == "true";

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Türkiye saati (UTC+3) için helper metod
        private DateTime GetTurkeyTime()
        {
            return DateTime.UtcNow.AddHours(3);
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            // Eğer zaten admin girişi yapılmışsa Dashboard'a yönlendir
            if (HasAdminAccess)
            {
                return RedirectToAction("Dashboard");
            }
            
            // Cache önleme header'ları
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminLogin(string Email, string Password)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a =>
                a.Email.ToLower() == Email.ToLower());

            if (admin == null)
            {
                ViewBag.Hata = "Hatalı e-posta veya şifre.";
                return View();
            }

            // Şifre doğrulama - hem hash'lenmiş hem de düz metin şifreleri destekle
            bool isPasswordValid = false;
            bool needsPasswordUpdate = false;
            
            try
            {
                // Hash formatını kontrol et (BCrypt hash'leri $2a$, $2b$, $2x$ veya $2y$ ile başlar)
                bool isHashed = !string.IsNullOrEmpty(admin.Sifre) && 
                               (admin.Sifre.StartsWith("$2a$") || 
                                admin.Sifre.StartsWith("$2b$") || 
                                admin.Sifre.StartsWith("$2x$") || 
                                admin.Sifre.StartsWith("$2y$"));

                if (isHashed)
                {
                    // Hash'lenmiş şifre ile kontrol et
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, admin.Sifre);
                }
                else
                {
                    // Düz metin şifre ile kontrol et (eski kayıtlar için)
                    isPasswordValid = admin.Sifre == Password;
                    
                    // Eğer şifre eşleşiyorsa, hash'leyip güncelle
                    if (isPasswordValid)
                    {
                        needsPasswordUpdate = true;
                    }
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Hash formatı bozuk - düz metin olarak dene
                isPasswordValid = admin.Sifre == Password;
                if (isPasswordValid)
                {
                    needsPasswordUpdate = true;
                }
            }
            catch (Exception ex)
            {
                // Diğer BCrypt hataları - düz metin olarak dene
                isPasswordValid = admin.Sifre == Password;
                if (isPasswordValid)
                {
                    needsPasswordUpdate = true;
                }
            }

            if (!isPasswordValid)
            {
                ViewBag.Hata = "Hatalı e-posta veya şifre.";
                return View();
            }

            // Eğer şifre düz metin ise, hash'leyip güncelle
            if (needsPasswordUpdate)
            {
                admin.Sifre = BCrypt.Net.BCrypt.HashPassword(Password);
                await _context.SaveChangesAsync();
            }

            // Kullanıcı session'larını temizle
            HttpContext.Session.Remove("UserGirisi");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserId");
            
            // Admin session'larını set et
            HttpContext.Session.SetString("AdminGirisi", "true");
            HttpContext.Session.SetString("AdminUserName", admin.Name);
            HttpContext.Session.SetString("AdminEmail", admin.Email);
            HttpContext.Session.SetInt32("AdminId", admin.Id);
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult AdminRegister()
        {
            if (HasAdminAccess) return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminRegister(string Name, string Email, string Password, string ConfirmPassword)
        {
            if (Password != ConfirmPassword)
            {
                ViewBag.Hata = "Şifreler eşleşmiyor.";
                return View();
            }

            // Email kontrolü - Admin tablosunda
            var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => 
                a.Email.ToLower() == Email.ToLower());
            
            if (existingAdmin != null)
            {
                ViewBag.Hata = "Bu e-posta adresi zaten kayıtlı.";
                return View();
            }

            // Email kontrolü - Kullanıcı tablosunda (kullanıcı olarak kayıtlıysa admin olamaz)
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == Email.ToLower());
            
            if (existingUser != null)
            {
                ViewBag.Hata = "Bu e-posta adresi kullanıcı olarak kayıtlı. Kullanıcı e-posta adresi ile admin kaydı yapılamaz.";
                return View();
            }

            // Şifreyi hash'le
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);
            
            var newAdmin = new Admin
            {
                Name = Name,
                Email = Email,
                Sifre = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            _context.Admins.Add(newAdmin);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
            return RedirectToAction("AdminLogin");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("AdminLogin");
            
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Id == adminId.Value);
            if (admin == null) return RedirectToAction("AdminLogin");
            
            return View(admin);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string Name, string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null) return RedirectToAction("AdminLogin");
            
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Id == adminId.Value);
            if (admin == null) return RedirectToAction("AdminLogin");

            // İsim güncelleme
            if (!string.IsNullOrEmpty(Name))
            {
                admin.Name = Name;
                HttpContext.Session.SetString("AdminUserName", Name);
            }

            // Şifre güncelleme
            if (!string.IsNullOrEmpty(NewPassword))
            {
                // Mevcut şifreyi kontrol et (hashlenmiş şifre ile)
                bool isCurrentPasswordValid = false;
                try
                {
                    // Hash formatını kontrol et
                    if (string.IsNullOrEmpty(admin.Sifre) || 
                        (!admin.Sifre.StartsWith("$2a$") && 
                         !admin.Sifre.StartsWith("$2b$") && 
                         !admin.Sifre.StartsWith("$2x$") && 
                         !admin.Sifre.StartsWith("$2y$")))
                    {
                        // Düz metin şifre - eski kayıtlar için
                        isCurrentPasswordValid = admin.Sifre == CurrentPassword;
                    }
                    else
                    {
                        // Hash'lenmiş şifre ile kontrol et
                        isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(CurrentPassword, admin.Sifre);
                    }
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    TempData["ErrorMessage"] = "Şifre formatı geçersiz. Lütfen yönetici ile iletişime geçin.";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Şifre doğrulama hatası: {ex.Message}";
                    return RedirectToAction("Profile");
                }

                if (!isCurrentPasswordValid)
                {
                    TempData["ErrorMessage"] = "Mevcut şifre hatalı.";
                    return RedirectToAction("Profile");
                }

                if (NewPassword != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Yeni şifreler eşleşmiyor.";
                    return RedirectToAction("Profile");
                }

                // Yeni şifreyi hash'le
                admin.Sifre = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profil bilgileri başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminGirisi");
            HttpContext.Session.Remove("AdminUserName");
            HttpContext.Session.Remove("AdminEmail");
            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Temel sayılar
            var tourCount = await _context.Tours.CountAsync();
            var bookingCount = await _context.Bookings.CountAsync();
            var paymentCount = await _context.Payments.CountAsync();
            var userCount = await _context.Users.CountAsync();

            // Ciro hesaplama mantığı:
            // - Tur tarihi geçmemişse: Sadece kapora ücreti ciroya sayılır
            // - Tur tarihi geçmişse: Kapora çıkarılır, toplam tur ücreti eklenir
            
            var today = DateTime.Today;
            
            // Aylık ciro hesaplama
            var monthlyRevenue = 0m;
            var monthlyBookingsList = await _context.Bookings
                .Where(b => b.CreatedAt >= startOfMonth && b.CreatedAt <= endOfMonth && b.IsDepositPaid)
                .ToListAsync();
            
            foreach (var booking in monthlyBookingsList)
            {
                if (booking.TourDate < today)
                {
                    // Tur geçmiş: Toplam tur ücreti ekle (kapora zaten ödenmişti, şimdi tam ücret sayılır)
                    monthlyRevenue += booking.TotalAmount;
                }
                else
                {
                    // Tur henüz yapılmadı: Sadece kapora ücreti
                    monthlyRevenue += booking.DepositAmount;
                }
            }

            // Toplam ciro hesaplama
            var totalRevenue = 0m;
            var allPaidBookings = await _context.Bookings
                .Where(b => b.IsDepositPaid)
                .ToListAsync();
            
            foreach (var booking in allPaidBookings)
            {
                if (booking.TourDate < today)
                {
                    // Tur geçmiş: Toplam tur ücreti ekle
                    totalRevenue += booking.TotalAmount;
                }
                else
                {
                    // Tur henüz yapılmadı: Sadece kapora ücreti
                    totalRevenue += booking.DepositAmount;
                }
            }

            // Bu ay yapılan rezervasyonlar
            var monthlyBookings = await _context.Bookings
                .Where(b => b.CreatedAt >= startOfMonth && b.CreatedAt <= endOfMonth)
                .CountAsync();

            // Yaklaşan turlar (gelecek 30 gün içinde)
            var upcomingTours = await _context.Bookings
                .Where(b => b.TourDate >= DateTime.Today && b.TourDate <= DateTime.Today.AddDays(30) && b.IsDepositPaid)
                .OrderBy(b => b.TourDate)
                .Take(10)
                .Select(b => new
                {
                    Id = b.Id,
                    TourName = b.TourName,
                    TourDate = b.TourDate,
                    Guests = b.Guests,
                    CustomerName = b.CustomerName,
                    TotalAmount = b.TotalAmount
                })
                .ToListAsync();

            // Son 6 ayın ciro verileri (grafik için) - Ocak dahil son 6 ay
            var monthlyRevenueData = new List<object>();
            var currentMonth = new DateTime(now.Year, now.Month, 1); // Şu anki ay (Ocak 2026)
            
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = currentMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var monthBookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= monthStart && b.CreatedAt <= monthEnd && b.IsDepositPaid)
                    .ToListAsync();
                
                var monthRevenue = 0m;
                foreach (var booking in monthBookings)
                {
                    if (booking.TourDate < today)
                    {
                        monthRevenue += booking.TotalAmount;
                    }
                    else
                    {
                        monthRevenue += booking.DepositAmount;
                    }
                }
                
                monthlyRevenueData.Add(new
                {
                    month = monthStart.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                    monthShort = monthStart.ToString("MMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                    revenue = monthRevenue
                });
            }
            
            // Detaylı ciro bilgisi (tıklanabilir kart için)
            var revenueDetails = await _context.Bookings
                .Where(b => b.IsDepositPaid)
                .OrderByDescending(b => b.TourDate)
                .Select(b => new
                {
                    TourName = b.TourName,
                    TourDate = b.TourDate,
                    Revenue = b.TourDate < today ? b.TotalAmount : b.DepositAmount,
                    IsCompleted = b.TourDate < today,
                    Guests = b.Guests,
                    CustomerName = b.CustomerName
                })
                .ToListAsync();

            // Kategori bazında tur sayıları - Son 6 ay boyunca yapılan rezervasyonlara göre
            var sixMonthsAgo = currentMonth.AddMonths(-5);
            var toursByCategory = await _context.Bookings
                .Where(b => b.CreatedAt >= sixMonthsAgo && b.IsDepositPaid)
                .Join(_context.Tours, 
                    booking => booking.TourId, 
                    tour => tour.Id, 
                    (booking, tour) => new { Category = tour.Category ?? "Diğer" })
                .GroupBy(x => x.Category)
                .Select(g => new { category = g.Key, count = g.Count() })
                .ToListAsync();

            ViewBag.TourCount = tourCount;
            ViewBag.BookingCount = bookingCount;
            ViewBag.PaymentCount = paymentCount;
            ViewBag.UserCount = userCount;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.MonthlyBookings = monthlyBookings;
            ViewBag.UpcomingTours = upcomingTours;
            ViewBag.MonthlyRevenueData = monthlyRevenueData;
            ViewBag.ToursByCategory = toursByCategory;
            ViewBag.RevenueDetails = revenueDetails;

            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> RevenueDetails()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var today = DateTime.Today;
            
            var revenueDetails = await _context.Bookings
                .Where(b => b.IsDepositPaid)
                .OrderByDescending(b => b.TourDate)
                .Select(b => new
                {
                    tourName = b.TourName,
                    tourDate = b.TourDate,
                    revenue = b.TourDate < today ? b.TotalAmount : b.DepositAmount,
                    isCompleted = b.TourDate < today,
                    guests = b.Guests,
                    customerName = b.CustomerName,
                    depositAmount = b.DepositAmount,
                    totalAmount = b.TotalAmount
                })
                .ToListAsync();
            
            return Json(revenueDetails);
        }

        public async Task<IActionResult> Tours()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var tours = await _context.Tours.ToListAsync();
            return View(tours);
        }

        [HttpGet]
        public IActionResult CreateTour()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            return View(new Tour());
        }

        [HttpPost]
        public async Task<IActionResult> CreateTour(Tour tour, IFormFile? imageFile, string? DurationHours)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            // Süre işleme - sadece saat sayısı giriliyor
            if (!string.IsNullOrEmpty(DurationHours) && double.TryParse(DurationHours, out double hours))
            {
                tour.Duration = TimeSpan.FromHours(hours);
            }
            else
            {
                tour.Duration = TimeSpan.FromHours(4); // Varsayılan
            }
            
            // Resim yükleme işlemi
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "tours");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                tour.ImageUrl = $"/images/tours/{uniqueFileName}";
            }
            else if (string.IsNullOrEmpty(tour.ImageUrl))
            {
                tour.ImageUrl = "/images/default-tour.jpg";
            }

            if (!ModelState.IsValid) return View(tour);

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{tour.Name}' turu başarıyla oluşturuldu.";
            return RedirectToAction("Tours");
        }

        [HttpGet]
        public async Task<IActionResult> EditTour(int id)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == id);
            if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost]
        public async Task<IActionResult> EditTour(Tour updated, IFormFile? imageFile, string? DurationHours)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            if (!ModelState.IsValid) return View(updated);

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == updated.Id);
            if (tour == null) return NotFound();

            // Süre işleme - sadece saat sayısı giriliyor
            if (!string.IsNullOrEmpty(DurationHours) && double.TryParse(DurationHours, out double hours))
            {
                tour.Duration = TimeSpan.FromHours(hours);
            }

            // Resim yükleme işlemi
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "tours");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Eski resmi sil (varsa ve varsayılan değilse)
                if (!string.IsNullOrEmpty(tour.ImageUrl) && 
                    tour.ImageUrl != "/images/default-tour.jpg" && 
                    tour.ImageUrl.StartsWith("/images/tours/"))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", tour.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                tour.ImageUrl = $"/images/tours/{uniqueFileName}";
            }
            else if (!string.IsNullOrEmpty(updated.ImageUrl))
            {
                tour.ImageUrl = updated.ImageUrl;
            }

            tour.Name = updated.Name;
            tour.Category = updated.Category;
            tour.Description = updated.Description;
            tour.PricePerPerson = updated.PricePerPerson;
            tour.Capacity = updated.Capacity;
            tour.IsActive = updated.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"'{tour.Name}' turu başarıyla güncellendi.";
            return RedirectToAction("Tours");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveTour(int id)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == id);
            if (tour == null)
            {
                TempData["ErrorMessage"] = "Tur bulunamadı.";
                return RedirectToAction("Tours");
            }

            // Aktif rezervasyon kontrolü (kapora ödenmiş ve tur tarihi geçmemiş)
            var activeBookings = await _context.Bookings
                .Where(b => b.TourId == id && b.IsDepositPaid && b.TourDate >= DateTime.Today)
                .CountAsync();
            
            if (activeBookings > 0)
            {
                TempData["ErrorMessage"] = $"Bu tur için {activeBookings} adet aktif rezervasyon mevcuttur. Tur silinemez. Lütfen önce rezervasyonları iptal edin.";
                return RedirectToAction("Tours");
            }

            // Turu referans eden booking'leri kontrol et ve otomatik iptal et
            var bookings = await _context.Bookings.Where(b => b.TourId == id).ToListAsync();
            if (bookings.Any())
            {
                // Önce tüm payment kayıtlarını topla ve sil
                var allBookingIds = bookings.Select(b => b.Id).ToList();
                var allPayments = await _context.Payments.Where(p => allBookingIds.Contains(p.BookingId)).ToListAsync();
                if (allPayments.Any())
                {
                    _context.Payments.RemoveRange(allPayments);
                    await _context.SaveChangesAsync(); // Payments'ları önce kaydet
                    _logger.LogInformation("TourId {TourId} için {Count} adet payment kaydı silindi.", id, allPayments.Count);
                }
                
                // Rezervasyonları iptal edilenler tablosuna taşı
                foreach (var booking in bookings)
                {
                    var cancelledBooking = new CancelledBooking
                    {
                        OriginalBookingId = booking.Id,
                        TourId = booking.TourId,
                        TourName = booking.TourName,
                        TourDate = booking.TourDate,
                        Guests = booking.Guests,
                        CustomerName = booking.CustomerName,
                        UserId = booking.UserId,
                        Email = booking.Email,
                        Phone = booking.Phone,
                        TotalAmount = booking.TotalAmount,
                        DepositAmount = booking.DepositAmount,
                        IsDepositPaid = booking.IsDepositPaid,
                        CreatedAt = booking.CreatedAt,
                        CancelledAt = GetTurkeyTime(),
                        CancelledBy = HttpContext.Session.GetString("AdminUserName") ?? "Admin",
                        CancellationReason = $"Tur silindiği için otomatik iptal edildi: {tour.Name}"
                    };
                    _context.CancelledBookings.Add(cancelledBooking);
                    _context.Bookings.Remove(booking);
                }
                
                // Booking'leri kaydet (CancelledBookings'e taşı ve Bookings'den sil)
                await _context.SaveChangesAsync();
            }

            try
            {
                // Silinen turu DeletedTours tablosuna ekle
                var deletedTour = new DeletedTour
                {
                    OriginalTourId = tour.Id,
                    Name = tour.Name,
                    Category = tour.Category,
                    Description = tour.Description,
                    PricePerPerson = tour.PricePerPerson,
                    Capacity = tour.Capacity,
                    Duration = tour.Duration,
                    ImageUrl = tour.ImageUrl,
                    IsActive = tour.IsActive,
                    DeletedAt = GetTurkeyTime(),
                    DeletedBy = HttpContext.Session.GetString("AdminUserName") ?? "Admin"
                };

                _context.DeletedTours.Add(deletedTour);
                
                // Orijinal turu sil
                _context.Tours.Remove(tour);
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"'{tour.Name}' turu başarıyla silindi ve silinen turlar listesine eklendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Tur silinirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("Tours");
        }

        public async Task<IActionResult> DeletedTours()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var deletedTours = await _context.DeletedTours.OrderByDescending(t => t.DeletedAt).ToListAsync();
            return View(deletedTours);
        }

        public async Task<IActionResult> RestoreTour(int id)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var deletedTour = await _context.DeletedTours.FirstOrDefaultAsync(t => t.Id == id);
            if (deletedTour == null)
            {
                TempData["ErrorMessage"] = "Silinen tur bulunamadı.";
                return RedirectToAction("DeletedTours");
            }

            try
            {
                // Yeni bir tur oluştur (yeni ID ile)
                var restoredTour = new Tour
                {
                    Name = deletedTour.Name,
                    Category = deletedTour.Category,
                    Description = deletedTour.Description,
                    PricePerPerson = deletedTour.PricePerPerson,
                    Capacity = deletedTour.Capacity,
                    Duration = deletedTour.Duration,
                    ImageUrl = deletedTour.ImageUrl,
                    IsActive = deletedTour.IsActive
                };

                _context.Tours.Add(restoredTour);
                _context.DeletedTours.Remove(deletedTour);
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"'{restoredTour.Name}' turu başarıyla geri yüklendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Tur geri yüklenirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("DeletedTours");
        }

        public async Task<IActionResult> Bookings()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var bookings = await _context.Bookings.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> BookingDetails(int id)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();
            return PartialView("_BookingDetails", booking);
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id, string? reason = null)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null)
            {
                // Zaten iptal edilmiş olabilir, kontrol et
                var alreadyCancelled = await _context.CancelledBookings.FirstOrDefaultAsync(cb => cb.OriginalBookingId == id);
                if (alreadyCancelled != null)
                {
                    TempData["ErrorMessage"] = "Bu rezervasyon zaten iptal edilmiş.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Rezervasyon bulunamadı.";
                }
                return RedirectToAction("Bookings");
            }

            try
            {
                // Önce ilgili payment kayıtlarını kontrol et ve sil (varsa) - Foreign key constraint için önce silinmeli
                var payments = await _context.Payments.Where(p => p.BookingId == id).ToListAsync();
                if (payments.Any())
                {
                    _context.Payments.RemoveRange(payments);
                    await _context.SaveChangesAsync(); // Payment'ları önce kaydet
                    _logger.LogInformation("BookingId {BookingId} için {Count} adet payment kaydı silindi.", id, payments.Count);
                }
                
                // İptal edilen rezervasyonu CancelledBookings tablosuna ekle
                var cancelledBooking = new CancelledBooking
                {
                    OriginalBookingId = booking.Id,
                    TourId = booking.TourId,
                    TourName = booking.TourName,
                    TourDate = booking.TourDate,
                    Guests = booking.Guests,
                    CustomerName = booking.CustomerName,
                    UserId = booking.UserId,
                    Email = booking.Email,
                    Phone = booking.Phone,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    IsDepositPaid = booking.IsDepositPaid,
                    CreatedAt = booking.CreatedAt,
                    CancelledAt = GetTurkeyTime(),
                    CancelledBy = HttpContext.Session.GetString("AdminUserName") ?? "Admin",
                    CancellationReason = reason ?? "Admin tarafından iptal edildi"
                };

                _context.CancelledBookings.Add(cancelledBooking);
                
                // Müşteriye bildirim oluştur
                if (booking.UserId.HasValue)
                {
                    var userMessage = $"'{booking.TourName}' turu için rezervasyonunuz admin tarafından iptal edilmiştir. Ücretiniz bankanıza göre 3 ila 7 iş günü içerisinde iade edilecektir.";
                    
                    var notification = new Notification
                    {
                        Title = "Rezervasyon İptal Edildi",
                        Message = userMessage,
                        Type = "danger",
                        UserId = booking.UserId,
                        AdminId = null, // Kullanıcı bildirimi
                        IsRead = false,
                        CreatedAt = GetTurkeyTime(),
                        RelatedBookingId = booking.Id,
                        CancellationReason = reason ?? "Admin tarafından iptal edildi"
                    };
                    _context.Notifications.Add(notification);
                }
                
                // Rezervasyonu sil
                _context.Bookings.Remove(booking);
                
                // Tüm değişiklikleri kaydet
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("BookingId {BookingId} başarıyla iptal edildi.", id);
                TempData["SuccessMessage"] = $"'{booking.CustomerName}' adlı müşterinin rezervasyonu başarıyla iptal edildi.";
            }
            catch (Exception ex)
            {
                // Inner exception'ı da göster
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" Detay: {ex.InnerException.Message}";
                }
                
                TempData["ErrorMessage"] = $"Rezervasyon iptal edilirken bir hata oluştu: {errorMessage}";
                
                // Log için
                _logger.LogError(ex, "Rezervasyon iptal hatası - BookingId: {BookingId}, InnerException: {InnerException}", 
                    id, ex.InnerException?.Message ?? "Yok");
            }

            return RedirectToAction("Bookings");
        }

        public async Task<IActionResult> CancellationRequests()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var requests = await _context.Bookings
                .Where(b => b.CancellationRequested)
                .OrderBy(b => b.CancellationRequestedAt)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveCancellation(int id, string? adminReason = null)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.CancellationRequested);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "İptal talebi bulunamadı.";
                return RedirectToAction("CancellationRequests");
            }

            try
            {
                // Payment kayıtlarını sil
                var payments = await _context.Payments.Where(p => p.BookingId == id).ToListAsync();
                if (payments.Any())
                {
                    _context.Payments.RemoveRange(payments);
                    await _context.SaveChangesAsync();
                }

                // İptal edilen rezervasyonu CancelledBookings tablosuna ekle
                var cancelledBooking = new CancelledBooking
                {
                    OriginalBookingId = booking.Id,
                    TourId = booking.TourId,
                    TourName = booking.TourName,
                    TourDate = booking.TourDate,
                    Guests = booking.Guests,
                    CustomerName = booking.CustomerName,
                    UserId = booking.UserId,
                    Email = booking.Email,
                    Phone = booking.Phone,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    IsDepositPaid = booking.IsDepositPaid,
                    CreatedAt = booking.CreatedAt,
                    CancelledAt = GetTurkeyTime(),
                    CancelledBy = HttpContext.Session.GetString("AdminUserName") ?? "Admin",
                    CancellationReason = $"Kullanıcı talebi: {booking.CancellationRequestReason}" + 
                                       (string.IsNullOrEmpty(adminReason) ? "" : $" | Admin notu: {adminReason}")
                };

                _context.CancelledBookings.Add(cancelledBooking);
                
                // Müşteriye bildirim oluştur
                if (booking.UserId.HasValue)
                {
                    var userMessage = $"'{booking.TourName}' turu için rezervasyon iptal talebiniz onaylandı ve rezervasyonunuz iptal edildi. Ücretiniz bankanıza göre 3 ila 7 iş günü içerisinde iade edilecektir.";
                    
                    var notification = new Notification
                    {
                        Title = "İptal Talebiniz Onaylandı",
                        Message = userMessage,
                        Type = "info",
                        UserId = booking.UserId,
                        IsRead = false,
                        CreatedAt = GetTurkeyTime(),
                        RelatedBookingId = booking.Id,
                        CancellationReason = adminReason ?? booking.CancellationRequestReason
                    };
                    _context.Notifications.Add(notification);
                }
                
                // Rezervasyonu sil
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Rezervasyon iptal talebi onaylandı ve rezervasyon iptal edildi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"İptal talebi onaylanırken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("CancellationRequests");
        }

        [HttpPost]
        public async Task<IActionResult> RejectCancellation(int id, string? reason = null)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.CancellationRequested);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "İptal talebi bulunamadı.";
                return RedirectToAction("CancellationRequests");
            }

            try
            {
                // İptal talebini iptal et
                booking.CancellationRequested = false;
                booking.CancellationRequestReason = null;
                booking.CancellationRequestedAt = null;
                
                // Müşteriye bildirim oluştur
                if (booking.UserId.HasValue)
                {
                    var userMessage = $"'{booking.TourName}' turu için rezervasyon iptal talebiniz reddedildi.";
                    
                    var notification = new Notification
                    {
                        Title = "İptal Talebiniz Reddedildi",
                        Message = userMessage,
                        Type = "warning",
                        UserId = booking.UserId,
                        AdminId = null, // Kullanıcı bildirimi
                        IsRead = false,
                        CreatedAt = GetTurkeyTime(),
                        RelatedBookingId = booking.Id,
                        CancellationReason = reason
                    };
                    _context.Notifications.Add(notification);
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "İptal talebi reddedildi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"İptal talebi reddedilirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("CancellationRequests");
        }

        public async Task<IActionResult> UpdateRequests()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var requests = await _context.Bookings
                .Where(b => b.UpdateRequested && b.UpdateRequestStatus == "Pending")
                .OrderBy(b => b.UpdateRequestedAt)
                .ToListAsync();
            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUpdate(int id, string? adminResponse = null)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UpdateRequested && b.UpdateRequestStatus == "Pending");
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Güncelleme talebi bulunamadı.";
                return RedirectToAction("UpdateRequests");
            }

            try
            {
                // Rezervasyonu güncelle
                booking.TourDate = booking.RequestedTourDate ?? booking.TourDate;
                booking.Guests = booking.RequestedGuests ?? booking.Guests;
                booking.UpdateRequestStatus = "Approved";
                booking.AdminUpdateResponse = adminResponse;
                
                // Müşteriye bildirim oluştur
                if (booking.UserId.HasValue)
                {
                    var userMessage = $"'{booking.TourName}' turu için rezervasyon güncelleme talebiniz onaylandı. Yeni tarih: {booking.TourDate:dd.MM.yyyy}, Yeni kişi sayısı: {booking.Guests}";
                    if (!string.IsNullOrEmpty(adminResponse))
                    {
                        userMessage += $" Admin notu: {adminResponse}";
                    }
                    
                    var notification = new Notification
                    {
                        Title = "Güncelleme Talebiniz Onaylandı",
                        Message = userMessage,
                        Type = "success",
                        UserId = booking.UserId,
                        IsRead = false,
                        CreatedAt = GetTurkeyTime(),
                        RelatedBookingId = booking.Id,
                        CancellationReason = adminResponse
                    };
                    _context.Notifications.Add(notification);
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Rezervasyon güncelleme talebi onaylandı ve rezervasyon güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Güncelleme talebi onaylanırken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("UpdateRequests");
        }

        [HttpPost]
        public async Task<IActionResult> RejectUpdate(int id, string? adminResponse = null)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UpdateRequested && b.UpdateRequestStatus == "Pending");
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Güncelleme talebi bulunamadı.";
                return RedirectToAction("UpdateRequests");
            }

            try
            {
                // Güncelleme talebini reddet
                booking.UpdateRequestStatus = "Rejected";
                booking.AdminUpdateResponse = adminResponse;
                
                // Müşteriye bildirim oluştur
                if (booking.UserId.HasValue)
                {
                    var userMessage = $"'{booking.TourName}' turu için rezervasyon güncelleme talebiniz reddedildi.";
                    if (!string.IsNullOrEmpty(adminResponse))
                    {
                        userMessage += $" Neden: {adminResponse}";
                    }
                    
                    var notification = new Notification
                    {
                        Title = "Güncelleme Talebiniz Reddedildi",
                        Message = userMessage,
                        Type = "warning",
                        UserId = booking.UserId,
                        AdminId = null,
                        IsRead = false,
                        CreatedAt = GetTurkeyTime(),
                        RelatedBookingId = booking.Id,
                        CancellationReason = adminResponse
                    };
                    _context.Notifications.Add(notification);
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Güncelleme talebi reddedildi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Güncelleme talebi reddedilirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("UpdateRequests");
        }

        public async Task<IActionResult> CancelledBookings()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var cancelledBookings = await _context.CancelledBookings.OrderByDescending(b => b.CancelledAt).ToListAsync();
            return View(cancelledBookings);
        }


        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!HasAdminAccess) return Json(new { notifications = new List<object>() });
            
            // Admin bildirimleri: Sadece admin'e gönderilen bildirimler (UserId null olmalı)
            // AdminId null ise tüm adminlere gider, AdminId dolu ise sadece o admin'e gider
            var adminId = HttpContext.Session.GetInt32("AdminId");
            
            var notifications = await _context.Notifications
                .Where(n => n.UserId == null) // Sadece admin'e gönderilen bildirimler
                .Where(n => n.AdminId == null || (adminId.HasValue && n.AdminId == adminId.Value))
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    createdAt = n.CreatedAt,
                    cancellationReason = n.CancellationReason,
                    relatedBookingId = n.RelatedBookingId,
                    phone = n.RelatedBookingId != null ? 
                        _context.Bookings.Where(b => b.Id == n.RelatedBookingId).Select(b => b.Phone).FirstOrDefault() : 
                        null
                })
                .ToListAsync();
            
            return Json(new { notifications });
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            if (!HasAdminAccess) return Json(new { success = false });
            
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            if (!HasAdminAccess) return Json(new { users = new List<object>() });
            
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    createdAt = u.CreatedAt
                })
                .ToListAsync();
            
            return Json(new { users });
        }
    }
}