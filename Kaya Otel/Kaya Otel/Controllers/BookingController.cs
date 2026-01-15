using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Kaya_Otel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kaya_Otel.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IIyzicoPaymentService _iyzicoPaymentService;
        private readonly ILogger<BookingController> _logger;
        private const decimal DepositRate = 0.20m;

        public BookingController(ApplicationDbContext context, IIyzicoPaymentService iyzicoPaymentService, ILogger<BookingController> logger)
        {
            _context = context;
            _iyzicoPaymentService = iyzicoPaymentService;
            _logger = logger;
        }

        // Türkiye saati (UTC+3) için helper metod
        private DateTime GetTurkeyTime()
        {
            return DateTime.UtcNow.AddHours(3);
        }

        [HttpGet]
        public async Task<IActionResult> Reserve(int tourId)
        {
            // Kullanıcı giriş kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Rezervasyon yapmak için üye girişi yapmalısınız.";
                TempData["ReturnUrl"] = Url.Action("Reserve", "Booking", new { tourId });
                return RedirectToAction("Register", "Account");
            }

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId && t.IsActive);
            if (tour == null) return NotFound();

            var model = new BookingRequestViewModel
            {
                TourId = tour.Id,
                TourDate = DateTime.Today,
                Guests = 2
            };

            ViewBag.Tour = tour;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Reserve(BookingRequestViewModel model)
        {
            // Kullanıcı giriş kontrolü
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            
            if (userId == null && string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Rezervasyon yapmak için üye girişi yapmalısınız.";
                return RedirectToAction("Register", "Account");
            }

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == model.TourId && t.IsActive);
            if (tour == null) return NotFound();

            // Tur kapasitesi kontrolü
            if (model.Guests > tour.Capacity)
            {
                ModelState.AddModelError(nameof(model.Guests), $"Bu tur için maksimum {tour.Capacity} kişi rezervasyon yapılabilir.");
            }

            if (model.Guests < 1)
            {
                ModelState.AddModelError(nameof(model.Guests), "Kişi sayısı en az 1 olmalıdır.");
            }

            // Seçilen tarihte kaporası ödenmiş rezervasyon var mı kontrol et (hangi tur olursa olsun)
            var hasPaidBookingOnDate = await _context.Bookings
                .AnyAsync(b => b.TourDate.Date == model.TourDate.Date && b.IsDepositPaid);

            if (hasPaidBookingOnDate)
            {
                ModelState.AddModelError(nameof(model.TourDate), "❌ Bu tarih için müsait değil. Bu tarihte kaporası ödenmiş bir rezervasyon bulunmaktadır.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tour = tour;
                return View(model);
            }

            // Tur ücreti sabittir, kişi sayısına göre değişmez
            var total = tour.PricePerPerson;
            // Kapora tur ücretinin %20'sidir
            var deposit = Math.Round(total * DepositRate, 2);

            // Kullanıcı giriş yapmışsa UserId'yi ekle (zaten yukarıda kontrol edildi)
            var loggedInUserId = HttpContext.Session.GetInt32("UserId");
            var loggedInUserEmail = HttpContext.Session.GetString("UserEmail");
            
            // Rezervasyon bilgilerini session'a kaydet (ödeme yapılmadan veritabanına kaydetme)
            HttpContext.Session.SetInt32("PendingBooking_TourId", tour.Id);
            HttpContext.Session.SetString("PendingBooking_TourName", tour.Name);
            HttpContext.Session.SetString("PendingBooking_TourDate", model.TourDate.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetInt32("PendingBooking_Guests", model.Guests);
            HttpContext.Session.SetString("PendingBooking_CustomerName", model.CustomerName);
            // Email'i session'dan al, yoksa form'dan al
            HttpContext.Session.SetString("PendingBooking_Email", !string.IsNullOrEmpty(loggedInUserEmail) ? loggedInUserEmail : model.Email);
            HttpContext.Session.SetString("PendingBooking_Phone", model.Phone);
            HttpContext.Session.SetString("PendingBooking_TotalAmount", total.ToString());
            HttpContext.Session.SetString("PendingBooking_DepositAmount", deposit.ToString());
            // UserId'yi mutlaka kaydet (varsa)
            if (loggedInUserId.HasValue)
            {
                HttpContext.Session.SetInt32("PendingBooking_UserId", loggedInUserId.Value);
            }
            else
            {
                // UserId yoksa session'dan kaldır (null değer kaydetme)
                HttpContext.Session.Remove("PendingBooking_UserId");
            }

            return RedirectToAction("Checkout");
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            // Session'dan rezervasyon bilgilerini al
            var tourId = HttpContext.Session.GetInt32("PendingBooking_TourId");
            if (!tourId.HasValue)
            {
                TempData["ErrorMessage"] = "Rezervasyon bilgileri bulunamadı. Lütfen rezervasyon formunu tekrar doldurun.";
                return RedirectToAction("Index", "Home");
            }

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId.Value);
            if (tour == null)
            {
                TempData["ErrorMessage"] = "Tur bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            // Session'dan bilgileri al ve Booking modeli oluştur (sadece görüntüleme için)
            var booking = new Booking
            {
                TourId = tourId.Value,
                UserId = HttpContext.Session.GetInt32("PendingBooking_UserId"),
                TourName = HttpContext.Session.GetString("PendingBooking_TourName") ?? tour.Name,
                TourDate = DateTime.Parse(HttpContext.Session.GetString("PendingBooking_TourDate") ?? DateTime.Today.ToString("yyyy-MM-dd")),
                Guests = HttpContext.Session.GetInt32("PendingBooking_Guests") ?? 1,
                CustomerName = HttpContext.Session.GetString("PendingBooking_CustomerName") ?? "",
                Email = HttpContext.Session.GetString("PendingBooking_Email") ?? "",
                Phone = HttpContext.Session.GetString("PendingBooking_Phone") ?? "",
                TotalAmount = decimal.Parse(HttpContext.Session.GetString("PendingBooking_TotalAmount") ?? "0"),
                DepositAmount = decimal.Parse(HttpContext.Session.GetString("PendingBooking_DepositAmount") ?? "0"),
                IsDepositPaid = false
            };

            ViewBag.TourPrice = tour.PricePerPerson;
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CompletePayment(string provider = "iyzico", string cardHolderName = "", string cardNumber = "", string expireMonth = "", string expireYear = "", string cvc = "")
        {
            // Session'dan rezervasyon bilgilerini al
            var tourId = HttpContext.Session.GetInt32("PendingBooking_TourId");
            if (!tourId.HasValue)
            {
                TempData["PaymentError"] = "Rezervasyon bilgileri bulunamadı. Lütfen rezervasyon formunu tekrar doldurun.";
                return RedirectToAction("Index", "Home");
            }

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId.Value && t.IsActive);
            if (tour == null)
            {
                TempData["PaymentError"] = "Tur bulunamadı veya aktif değil.";
                return RedirectToAction("Index", "Home");
            }

            // Session'dan UserId'yi al (hem PendingBooking_UserId hem de doğrudan UserId'yi kontrol et)
            var pendingUserId = HttpContext.Session.GetInt32("PendingBooking_UserId");
            var directUserId = HttpContext.Session.GetInt32("UserId");
            var finalUserId = pendingUserId ?? directUserId;
            
            // Session'dan Email'i al (hem PendingBooking_Email hem de doğrudan UserEmail'i kontrol et)
            var pendingEmail = HttpContext.Session.GetString("PendingBooking_Email");
            var directUserEmail = HttpContext.Session.GetString("UserEmail");
            var finalEmail = !string.IsNullOrEmpty(pendingEmail) ? pendingEmail : directUserEmail;
            
            // Rezervasyonu oluştur (ödeme yapılmadan önce oluşturulmuyordu, şimdi ödeme sırasında oluşturulacak)
            var booking = new Booking
            {
                TourId = tourId.Value,
                UserId = finalUserId,
                TourName = HttpContext.Session.GetString("PendingBooking_TourName") ?? tour.Name,
                TourDate = DateTime.Parse(HttpContext.Session.GetString("PendingBooking_TourDate") ?? DateTime.Today.ToString("yyyy-MM-dd")),
                Guests = HttpContext.Session.GetInt32("PendingBooking_Guests") ?? 1,
                CustomerName = HttpContext.Session.GetString("PendingBooking_CustomerName") ?? "",
                Email = finalEmail ?? "",
                Phone = HttpContext.Session.GetString("PendingBooking_Phone") ?? "",
                TotalAmount = decimal.Parse(HttpContext.Session.GetString("PendingBooking_TotalAmount") ?? "0"),
                DepositAmount = decimal.Parse(HttpContext.Session.GetString("PendingBooking_DepositAmount") ?? "0"),
                IsDepositPaid = false,
                CreatedAt = GetTurkeyTime()
            };

            // Rezervasyonu veritabanına ekle (henüz ödeme yapılmadı, ödeme başarılı olursa IsDepositPaid = true yapılacak)
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Kart bilgileri validasyonu
            if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(expireMonth) ||
                string.IsNullOrWhiteSpace(expireYear) || string.IsNullOrWhiteSpace(cvc))
            {
                TempData["PaymentError"] = "Lütfen tüm kart bilgilerini eksiksiz doldurunuz.";
                return RedirectToAction("Checkout", new { bookingId = booking.Id });
            }

            // Sadece iyzico için gerçek ödeme isteği gönder
            if (provider == "iyzico")
            {
                _logger.LogInformation("Iyzico payment request started for booking {BookingId}, Amount: {Amount}", booking.Id, booking.DepositAmount);

                var iyzicoResult = await _iyzicoPaymentService.ChargeDepositAsync(booking, cardHolderName, cardNumber, expireMonth, expireYear, cvc);

                _logger.LogInformation("Iyzico payment result for booking {BookingId} - Success: {Success}, Status: {Status}, TransactionId: {TransactionId}, ErrorMessage: {ErrorMessage}, RawResult: {RawResult}",
                    booking.Id,
                    iyzicoResult.Success,
                    iyzicoResult.Status,
                    iyzicoResult.TransactionId,
                    iyzicoResult.ErrorMessage,
                    iyzicoResult.RawResult);

                if (!iyzicoResult.Success)
                {
                    _logger.LogWarning("Iyzico payment failed for booking {BookingId}. Status: {Status}, Error: {Error}", booking.Id, iyzicoResult.Status, iyzicoResult.ErrorMessage);
                    
                    // Ödeme başarısız oldu, rezervasyonu sil
                    _context.Bookings.Remove(booking);
                    await _context.SaveChangesAsync();
                    
                    TempData["PaymentError"] = $"Ödeme işlemi başarısız oldu: {iyzicoResult.ErrorMessage}. Lütfen kart bilgilerinizi kontrol edip tekrar deneyiniz.";
                    return RedirectToAction("Checkout");
                }

                booking.IsDepositPaid = true;

                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = booking.DepositAmount,
                    Provider = provider,
                    Status = iyzicoResult.Status,
                    TransactionId = iyzicoResult.TransactionId,
                    PaidAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                
                // Admin'e yeni rezervasyon bildirimi gönder
                var adminNotification = new Notification
                {
                    Title = "Yeni Rezervasyon",
                    Message = $"{booking.CustomerName} ({booking.Email}) adlı müşteri '{booking.TourName}' turu için {booking.Guests} kişilik rezervasyon yaptı. Tarih: {booking.TourDate:dd.MM.yyyy}",
                    Type = "info",
                    AdminId = null, // Tüm adminlere gider
                    UserId = null, // Admin bildirimi
                    IsRead = false,
                    CreatedAt = GetTurkeyTime(),
                    RelatedBookingId = booking.Id
                };
                _context.Notifications.Add(adminNotification);
                
                await _context.SaveChangesAsync();

                // Session'daki pending booking bilgilerini temizle
                HttpContext.Session.Remove("PendingBooking_TourId");
                HttpContext.Session.Remove("PendingBooking_TourName");
                HttpContext.Session.Remove("PendingBooking_TourDate");
                HttpContext.Session.Remove("PendingBooking_Guests");
                HttpContext.Session.Remove("PendingBooking_CustomerName");
                HttpContext.Session.Remove("PendingBooking_Email");
                HttpContext.Session.Remove("PendingBooking_Phone");
                HttpContext.Session.Remove("PendingBooking_TotalAmount");
                HttpContext.Session.Remove("PendingBooking_DepositAmount");
                HttpContext.Session.Remove("PendingBooking_UserId");

                _logger.LogInformation("Payment saved successfully for booking {BookingId}, TransactionId: {TransactionId}", booking.Id, payment.TransactionId);

                return RedirectToAction("Success", new { bookingId = booking.Id });
            }

            // Diğer sağlayıcılar için eski simülasyon davranışı
            booking.IsDepositPaid = true;

            var simulatedPayment = new Payment
            {
                BookingId = booking.Id,
                Amount = booking.DepositAmount,
                Provider = provider,
                Status = "Paid",
                TransactionId = Guid.NewGuid().ToString("N"),
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(simulatedPayment);
            
            // Admin'e yeni rezervasyon bildirimi gönder
            var adminNotification2 = new Notification
            {
                Title = "Yeni Rezervasyon",
                Message = $"{booking.CustomerName} ({booking.Email}) adlı müşteri '{booking.TourName}' turu için {booking.Guests} kişilik rezervasyon yaptı. Tarih: {booking.TourDate:dd.MM.yyyy}",
                Type = "info",
                AdminId = null, // Tüm adminlere gider
                UserId = null, // Admin bildirimi
                IsRead = false,
                CreatedAt = GetTurkeyTime(),
                RelatedBookingId = booking.Id
            };
            _context.Notifications.Add(adminNotification2);
            
            await _context.SaveChangesAsync();

            // Session'daki pending booking bilgilerini temizle
            HttpContext.Session.Remove("PendingBooking_TourId");
            HttpContext.Session.Remove("PendingBooking_TourName");
            HttpContext.Session.Remove("PendingBooking_TourDate");
            HttpContext.Session.Remove("PendingBooking_Guests");
            HttpContext.Session.Remove("PendingBooking_CustomerName");
            HttpContext.Session.Remove("PendingBooking_Email");
            HttpContext.Session.Remove("PendingBooking_Phone");
            HttpContext.Session.Remove("PendingBooking_TotalAmount");
            HttpContext.Session.Remove("PendingBooking_DepositAmount");
            HttpContext.Session.Remove("PendingBooking_UserId");

            return RedirectToAction("Success", new { bookingId = booking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Success(int bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            ViewBag.Message = "Kapora ödemeniz başarıyla alındı.";
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingDates()
        {
            var bookings = await _context.Bookings.ToListAsync();

            var paidDates = bookings
                .Where(b => b.IsDepositPaid)
                .Select(b => b.TourDate.Date.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToList();

            var unpaidDates = bookings
                .Where(b => !b.IsDepositPaid)
                .Select(b => b.TourDate.Date.ToString("yyyy-MM-dd"))
                .Distinct()
                .ToList();

            return Json(new
            {
                paidDates = paidDates,
                unpaidDates = unpaidDates
            });
        }
    }
}

