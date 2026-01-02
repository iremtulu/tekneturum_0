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

        [HttpGet]
        public async Task<IActionResult> Reserve(int tourId)
        {
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == tourId);
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
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == model.TourId);
            if (tour == null) return NotFound();

            // Seçilen tarihte kaporası ödenmiş rezervasyon var mı kontrol et (hangi tur olursa olsun)
            var hasPaidBookingOnDate = await _context.Bookings
                .AnyAsync(b => b.TourDate.Date == model.TourDate.Date && b.IsDepositPaid);

            if (hasPaidBookingOnDate)
            {
                ModelState.AddModelError(nameof(model.TourDate), "Müsait değil");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tour = tour;
                return View(model);
            }

            var total = model.Guests * tour.PricePerPerson;
            var deposit = Math.Round(tour.PricePerPerson * DepositRate, 2);

            var booking = new Booking
            {
                TourId = tour.Id,
                TourName = tour.Name,
                TourDate = model.TourDate,
                Guests = model.Guests,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                TotalAmount = total,
                DepositAmount = deposit,
                IsDepositPaid = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Checkout", new { bookingId = booking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == booking.TourId);
            if (tour != null)
            {
                ViewBag.TourPrice = tour.PricePerPerson;
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CompletePayment(int bookingId, string provider = "iyzico")
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            // Sadece iyzico için gerçek ödeme isteği gönder
            if (provider == "iyzico")
            {
                var iyzicoResult = await _iyzicoPaymentService.ChargeDepositAsync(booking);

                if (!iyzicoResult.Success)
                {
                    _logger.LogWarning("Iyzico payment failed for booking {BookingId}. Status: {Status}, Error: {Error}", booking.Id, iyzicoResult.Status, iyzicoResult.ErrorMessage);
                    TempData["PaymentError"] = "Ödeme işlemi başarısız oldu. Lütfen daha sonra tekrar deneyiniz.";
                    return RedirectToAction("Checkout", new { bookingId = booking.Id });
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
                await _context.SaveChangesAsync();

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
            await _context.SaveChangesAsync();

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

