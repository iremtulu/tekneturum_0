using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Kaya_Otel.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private bool HasUserAccess => HttpContext.Session.GetString("UserGirisi") == "true";

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Türkiye saati (UTC+3) için helper metod
        private DateTime GetTurkeyTime()
        {
            return DateTime.UtcNow.AddHours(3);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HasUserAccess) return RedirectToAction("MyBookings");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string Name, string Email, string Password, string ConfirmPassword)
        {
            try
        {
                // Validasyon
                if (string.IsNullOrWhiteSpace(Name))
                {
                    ViewBag.Hata = "Ad alanı boş bırakılamaz.";
                    return View();
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    ViewBag.Hata = "E-posta alanı boş bırakılamaz.";
                    return View();
                }

                if (string.IsNullOrWhiteSpace(Password))
                {
                    ViewBag.Hata = "Şifre alanı boş bırakılamaz.";
                    return View();
                }

                if (Password != ConfirmPassword)
                {
                    ViewBag.Hata = "Şifreler eşleşmiyor.";
                    return View();
                }

                // Email kontrolü - Kullanıcı tablosunda
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == Email.ToLower());
                
                if (existingUser != null)
                {
                    ViewBag.Hata = "Bu e-posta adresi zaten kayıtlı.";
                    return View();
                }

                // Email kontrolü - Admin tablosunda (admin olarak kayıtlıysa kullanıcı olamaz)
                var existingAdmin = await _context.Admins.FirstOrDefaultAsync(a => 
                    a.Email.ToLower() == Email.ToLower());
                
                if (existingAdmin != null)
                {
                    ViewBag.Hata = "Bu e-posta adresi admin olarak kayıtlı. Admin e-posta adresi ile kullanıcı kaydı yapılamaz.";
                    return View();
                }

                // Şifreyi hashle
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);
                
                var newUser = new user
                {
                    Name = Name.Trim(),
                    Email = Email.Trim().ToLower(),
                    Password = hashedPassword,
                    CreatedAt = GetTurkeyTime()
                };

                _context.Users.Add(newUser);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.Hata = "Kayıt işlemi başarısız oldu. Lütfen tekrar deneyin.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Hata = $"Kayıt işlemi sırasında bir hata oluştu: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HasUserAccess) return RedirectToAction("MyBookings");
            
            // Cache önleme header'ları
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    ViewBag.Hata = "E-posta ve şifre alanları boş bırakılamaz.";
                    return View();
                }

                // Email ile kullanıcıyı bul
                var foundUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == Email.Trim().ToLower());

                if (foundUser == null)
                {
                    ViewBag.Hata = "Geçersiz e-posta veya şifre.";
                    return View();
                }

                // Şifre doğrulama - hem hash'lenmiş hem de düz metin şifreleri destekle
                bool isPasswordValid = false;
                bool needsPasswordUpdate = false;
                
                try
                {
                    // Hash formatını kontrol et (BCrypt hash'leri $2a$, $2b$, $2x$ veya $2y$ ile başlar)
                    bool isHashed = !string.IsNullOrEmpty(foundUser.Password) && 
                                   (foundUser.Password.StartsWith("$2a$") || 
                                    foundUser.Password.StartsWith("$2b$") || 
                                    foundUser.Password.StartsWith("$2x$") || 
                                    foundUser.Password.StartsWith("$2y$"));

                    if (isHashed)
                    {
                        // Hash'lenmiş şifre ile kontrol et
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, foundUser.Password);
                    }
                    else
                    {
                        // Düz metin şifre ile kontrol et (eski kayıtlar için)
                        isPasswordValid = foundUser.Password == Password;
                        
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
                    isPasswordValid = foundUser.Password == Password;
                    if (isPasswordValid)
                    {
                        needsPasswordUpdate = true;
                    }
                }
                catch (Exception ex)
                {
                    // Diğer BCrypt hataları - düz metin olarak dene
                    isPasswordValid = foundUser.Password == Password;
                    if (isPasswordValid)
                    {
                        needsPasswordUpdate = true;
                    }
                }

                if (!isPasswordValid)
                {
                    ViewBag.Hata = "Geçersiz e-posta veya şifre.";
                    return View();
                }

                // Eğer şifre düz metin ise, hash'leyip güncelle
                if (needsPasswordUpdate)
                {
                    foundUser.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                    await _context.SaveChangesAsync();
                }

                // Admin session'larını temizle
                HttpContext.Session.Remove("AdminGirisi");
                HttpContext.Session.Remove("AdminUserName");
                HttpContext.Session.Remove("AdminEmail");
                HttpContext.Session.Remove("AdminId");
                
                // Kullanıcı session'larını set et
                HttpContext.Session.SetString("UserGirisi", "true");
                HttpContext.Session.SetString("UserName", foundUser.Name);
                HttpContext.Session.SetString("UserEmail", foundUser.Email);
                HttpContext.Session.SetInt32("UserId", foundUser.Id);
                
                TempData["SuccessMessage"] = "Başarıyla giriş yaptınız.";
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                ViewBag.Hata = $"Giriş işlemi sırasında bir hata oluştu: {ex.Message}";
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserGirisi");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("UserEmail");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> MyBookings()
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            // Kullanıcının aktif rezervasyonlarını getir (UserId veya Email ile)
            var bookings = await _context.Bookings
                .Where(b => (userId.HasValue && b.UserId == userId.Value) || 
                           (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(b.Email) && b.Email.ToLower().Trim() == userEmail.ToLower().Trim()))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        public async Task<IActionResult> CancelledBookings()
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            // Kullanıcının iptal edilen rezervasyonlarını getir (UserId veya Email ile)
            // Her iki durumu da kontrol et ve birleştir
            var allCancelledBookings = new List<CancelledBooking>();
            
            // UserId ile kontrol et
            if (userId.HasValue)
            {
                var userIdBookings = await _context.CancelledBookings
                    .Where(cb => cb.UserId.HasValue && cb.UserId == userId.Value)
                    .ToListAsync();
                allCancelledBookings.AddRange(userIdBookings);
            }
            
            // Email ile kontrol et (duplicate kontrolü ile)
            // Email kontrolünü her zaman yap (UserId ile bulunamasa bile)
            if (!string.IsNullOrEmpty(userEmail))
            {
                var emailLower = userEmail.ToLower().Trim();
                var emailBookings = await _context.CancelledBookings
                    .Where(cb => !string.IsNullOrEmpty(cb.Email) && cb.Email.ToLower().Trim() == emailLower)
                    .ToListAsync();
                
                // Duplicate kontrolü: Eğer aynı kayıt hem UserId hem Email ile bulunduysa, sadece bir kez ekle
                var existingIds = allCancelledBookings.Select(cb => cb.Id).ToHashSet();
                allCancelledBookings.AddRange(emailBookings.Where(cb => !existingIds.Contains(cb.Id)));
            }
            
            // Tarihe göre sırala
            var cancelledBookings = allCancelledBookings
                .OrderByDescending(cb => cb.CancelledAt)
                .ToList();

            return View(cancelledBookings);
        }

        [HttpPost]
        public async Task<IActionResult> RequestCancellation(int id, string reason)
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Lütfen iptal nedeninizi belirtin.";
                return RedirectToAction("MyBookings");
            }

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => 
                b.Id == id && 
                ((userId.HasValue && b.UserId == userId.Value) || 
                 (!string.IsNullOrEmpty(userEmail) && b.Email.ToLower() == userEmail.ToLower())));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Rezervasyon bulunamadı veya bu rezervasyona erişim yetkiniz yok.";
                return RedirectToAction("MyBookings");
            }

            if (booking.CancellationRequested)
            {
                TempData["ErrorMessage"] = "Bu rezervasyon için zaten bir iptal talebi gönderilmiş.";
                return RedirectToAction("MyBookings");
            }

            try
            {
                // İptal talebini kaydet
                booking.CancellationRequested = true;
                booking.CancellationRequestReason = reason;
                booking.CancellationRequestedAt = DateTime.UtcNow;
                
                // Admin'e bildirim oluştur
                var adminMessage = $"{booking.CustomerName} ({booking.Email}) adlı müşteri '{booking.TourName}' turu için rezervasyon iptal talebi gönderdi.";
                
                var notification = new Notification
                {
                    Title = "Yeni Rezervasyon İptal Talebi",
                    Message = adminMessage,
                    Type = "warning",
                    AdminId = null, // Tüm adminlere gider
                    IsRead = false,
                    CreatedAt = GetTurkeyTime(),
                    RelatedBookingId = booking.Id,
                    CancellationReason = reason
                };
                _context.Notifications.Add(notification);
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "İptal talebiniz admin'e gönderildi. Onaylandıktan sonra rezervasyonunuz iptal edilecektir.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"İptal talebi gönderilirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        public async Task<IActionResult> CancelCancellationRequest(int id)
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => 
                b.Id == id && 
                ((userId.HasValue && b.UserId == userId.Value) || 
                 (!string.IsNullOrEmpty(userEmail) && b.Email.ToLower() == userEmail.ToLower())));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Rezervasyon bulunamadı veya bu rezervasyona erişim yetkiniz yok.";
                return RedirectToAction("MyBookings");
            }

            if (!booking.CancellationRequested)
            {
                TempData["ErrorMessage"] = "Bu rezervasyon için iptal talebi bulunmamaktadır.";
                return RedirectToAction("MyBookings");
            }

            try
            {
                // İptal talebini iptal et
                booking.CancellationRequested = false;
                booking.CancellationRequestReason = null;
                booking.CancellationRequestedAt = null;
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "İptal talebiniz iptal edildi. Rezervasyonunuz aktif durumda.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"İptal talebinden vazgeçilirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction("MyBookings");
        }

        [HttpGet]
        public async Task<IActionResult> EditBooking(int id)
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => 
                b.Id == id && 
                ((userId.HasValue && b.UserId == userId.Value) || 
                 (!string.IsNullOrEmpty(userEmail) && b.Email.ToLower() == userEmail.ToLower())));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Rezervasyon bulunamadı veya bu rezervasyona erişim yetkiniz yok.";
                return RedirectToAction("MyBookings");
            }

            // Eğer güncelleme talebi bekleniyorsa, kullanıcıya bilgi ver
            if (booking.UpdateRequested && booking.UpdateRequestStatus == "Pending")
            {
                TempData["InfoMessage"] = "Bu rezervasyon için zaten bir güncelleme talebi gönderilmiş ve admin onayı bekleniyor.";
            }

            // Tur bilgisini al (kapasite için)
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == booking.TourId);
            if (tour != null)
            {
                ViewBag.TourCapacity = tour.Capacity;
            }

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> CheckDateAvailability(int bookingId, string date)
        {
            if (!HasUserAccess) return Json(new { available = false, message = "Yetkisiz erişim" });
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return Json(new { available = false, message = "Giriş yapmalısınız" });

            if (!DateTime.TryParse(date, out DateTime selectedDate))
            {
                return Json(new { available = false, message = "Geçersiz tarih formatı" });
            }

            // Kendi rezervasyonunu hariç tutarak, seçilen tarihte kaporası ödenmiş başka bir rezervasyon var mı kontrol et
            var hasOtherPaidBooking = await _context.Bookings
                .AnyAsync(b => b.Id != bookingId && 
                              b.TourDate.Date == selectedDate.Date && 
                              b.IsDepositPaid);

            if (hasOtherPaidBooking)
            {
                return Json(new { available = false, message = "Müsait değil" });
            }

            return Json(new { available = true, message = "Müsait" });
        }

        [HttpPost]
        public async Task<IActionResult> RequestUpdate(int id, DateTime requestedTourDate, int requestedGuests, string reason)
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Lütfen güncelleme nedeninizi belirtin.";
                return RedirectToAction("EditBooking", new { id });
            }

            if (requestedGuests < 1)
            {
                TempData["ErrorMessage"] = "Kişi sayısı en az 1 olmalıdır.";
                return RedirectToAction("EditBooking", new { id });
            }

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => 
                b.Id == id && 
                ((userId.HasValue && b.UserId == userId.Value) || 
                 (!string.IsNullOrEmpty(userEmail) && b.Email.ToLower() == userEmail.ToLower())));

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Rezervasyon bulunamadı veya bu rezervasyona erişim yetkiniz yok.";
                return RedirectToAction("MyBookings");
            }

            // Tur kapasitesi kontrolü
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == booking.TourId);
            if (tour != null && requestedGuests > tour.Capacity)
            {
                TempData["ErrorMessage"] = $"Bu tur için maksimum {tour.Capacity} kişi rezervasyon yapılabilir.";
                return RedirectToAction("EditBooking", new { id });
            }

            // Seçilen tarihte kendi rezervasyonu hariç, kaporası ödenmiş başka bir rezervasyon var mı kontrol et
            var hasOtherPaidBooking = await _context.Bookings
                .AnyAsync(b => b.Id != id && 
                              b.TourDate.Date == requestedTourDate.Date && 
                              b.IsDepositPaid);

            if (hasOtherPaidBooking)
            {
                TempData["ErrorMessage"] = "Seçtiğiniz tarih için müsait değil. Bu tarihte kaporası ödenmiş başka bir rezervasyon bulunmaktadır.";
                return RedirectToAction("EditBooking", new { id });
            }

            if (booking.UpdateRequested && booking.UpdateRequestStatus == "Pending")
            {
                TempData["ErrorMessage"] = "Bu rezervasyon için zaten bir güncelleme talebi gönderilmiş ve admin onayı bekleniyor.";
                return RedirectToAction("MyBookings");
            }

            try
            {
                // Güncelleme talebini kaydet
                booking.UpdateRequested = true;
                booking.UpdateRequestReason = reason;
                booking.UpdateRequestedAt = DateTime.UtcNow;
                booking.UpdateRequestStatus = "Pending";
                booking.RequestedTourDate = requestedTourDate;
                booking.RequestedGuests = requestedGuests;
                
                // Admin'e bildirim oluştur
                var adminMessage = $"{booking.CustomerName} ({booking.Email}) adlı müşteri '{booking.TourName}' turu için rezervasyon güncelleme talebi gönderdi. Mevcut: {booking.TourDate:dd.MM.yyyy} - {booking.Guests} kişi, Talep: {requestedTourDate:dd.MM.yyyy} - {requestedGuests} kişi.";
                
                var notification = new Notification
                {
                    Title = "Yeni Rezervasyon Güncelleme Talebi",
                    Message = adminMessage,
                    Type = "info",
                    AdminId = null, // Tüm adminlere gider
                    IsRead = false,
                    CreatedAt = GetTurkeyTime(),
                    RelatedBookingId = booking.Id,
                    CancellationReason = reason
                };
                _context.Notifications.Add(notification);
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Güncelleme talebiniz başarıyla gönderildi. Admin onayı bekleniyor.";
                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Güncelleme talebi gönderilirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("EditBooking", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!HasUserAccess) return Json(new { notifications = new List<object>() });
            
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { notifications = new List<object>() });
            
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    createdAt = n.CreatedAt,
                    cancellationReason = n.CancellationReason,
                    relatedBookingId = n.RelatedBookingId
                })
                .ToListAsync();
            
            return Json(new { notifications });
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            if (!HasUserAccess) return Json(new { success = false });
            
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
        public async Task<IActionResult> BookingDetails(int id, bool isCancelled = false)
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (isCancelled)
            {
                // İptal edilmiş rezervasyon detayları
                var cancelledBooking = await _context.CancelledBookings.FirstOrDefaultAsync(cb => 
                    cb.OriginalBookingId == id && 
                    (!string.IsNullOrEmpty(userEmail) && cb.Email.ToLower() == userEmail.ToLower()));
                
                if (cancelledBooking == null) return NotFound();
                
                ViewBag.IsCancelled = true;
                ViewBag.CancelledBooking = cancelledBooking;
                
                // Booking modeline benzer bir yapı oluştur (partial view için)
                var bookingModel = new Booking
                {
                    Id = cancelledBooking.OriginalBookingId,
                    TourName = cancelledBooking.TourName,
                    TourDate = cancelledBooking.TourDate,
                    Guests = cancelledBooking.Guests,
                    CustomerName = cancelledBooking.CustomerName,
                    Email = cancelledBooking.Email,
                    Phone = cancelledBooking.Phone,
                    TotalAmount = cancelledBooking.TotalAmount,
                    DepositAmount = cancelledBooking.DepositAmount,
                    IsDepositPaid = cancelledBooking.IsDepositPaid,
                    CreatedAt = cancelledBooking.CreatedAt
                };
                
                return PartialView("_BookingDetails", bookingModel);
            }
            else
            {
                // Aktif rezervasyon detayları
                var booking = await _context.Bookings.FirstOrDefaultAsync(b => 
                    b.Id == id && 
                    ((userId.HasValue && b.UserId == userId.Value) || 
                     (!string.IsNullOrEmpty(userEmail) && b.Email.ToLower() == userEmail.ToLower())));
                
                if (booking == null) return NotFound();
                
                ViewBag.IsCancelled = false;
                return PartialView("_BookingDetails", booking);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            
            var user = await _context.Users
                .Where(u => u.Id == userId.Value)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();
            
            if (user == null) return RedirectToAction("Login");
            
            // Password alanı olmadan model oluştur
            var userModel = new user
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
            
            return View(userModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string Name, string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (!HasUserAccess) return RedirectToAction("Login");
            
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user == null) return RedirectToAction("Login");

            // İsim güncelleme
            if (!string.IsNullOrEmpty(Name))
            {
                user.Name = Name;
                HttpContext.Session.SetString("UserName", Name);
            }

            // Şifre güncelleme
            if (!string.IsNullOrEmpty(NewPassword))
            {
                // Mevcut şifreyi kontrol et (hashlenmiş şifre ile)
                bool isCurrentPasswordValid = false;
                try
                {
                    // Hash formatını kontrol et
                    if (string.IsNullOrEmpty(user.Password) || 
                        (!user.Password.StartsWith("$2a$") && 
                         !user.Password.StartsWith("$2b$") && 
                         !user.Password.StartsWith("$2x$") && 
                         !user.Password.StartsWith("$2y$")))
                    {
                        TempData["ErrorMessage"] = "Mevcut şifre formatı geçersiz. Lütfen yönetici ile iletişime geçin.";
                        return RedirectToAction("Profile");
                    }

                    isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(CurrentPassword, user.Password);
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

                // Yeni şifreyi hashle
                user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profil bilgileri başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }
    }
}
