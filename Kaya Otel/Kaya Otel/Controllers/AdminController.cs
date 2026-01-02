using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kaya_Otel.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private bool HasAdminAccess => HttpContext.Session.GetString("AdminGirisi") == "true";

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AdminLogin(string UserName, string Password)
        {
            var admins = await _context.Admins.ToListAsync();
            var admin = admins.FirstOrDefault(a =>
                a.UserName.Equals(UserName, StringComparison.OrdinalIgnoreCase) &&
                a.Sifre == Password);

            if (admin != null)
            {
                HttpContext.Session.SetString("AdminGirisi", "true");
                return RedirectToAction("Dashboard");
            }

            ViewBag.Hata = "Hatalı giriş yaptınız.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminGirisi");
            return RedirectToAction("AdminLogin");
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");

            ViewBag.TourCount = await _context.Tours.CountAsync();
            ViewBag.BookingCount = await _context.Bookings.CountAsync();
            ViewBag.PaymentCount = await _context.Payments.CountAsync();

            return View();
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
        public async Task<IActionResult> CreateTour(Tour tour)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            if (!ModelState.IsValid) return View(tour);

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> EditTour(Tour updated)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            if (!ModelState.IsValid) return View(updated);

            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == updated.Id);
            if (tour == null) return NotFound();

            tour.Name = updated.Name;
            tour.Category = updated.Category;
            tour.Description = updated.Description;
            tour.PricePerPerson = updated.PricePerPerson;
            tour.Capacity = updated.Capacity;
            tour.Duration = updated.Duration;
            tour.ImageUrl = updated.ImageUrl;
            tour.IsActive = updated.IsActive;

            await _context.SaveChangesAsync();

            return RedirectToAction("Tours");
        }

        public async Task<IActionResult> RemoveTour(int id)
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == id);
            if (tour != null)
            {
                _context.Tours.Remove(tour);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Tours");
        }

        public async Task<IActionResult> Bookings()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var bookings = await _context.Bookings.ToListAsync();
            return View(bookings);
        }

        public async Task<IActionResult> Payments()
        {
            if (!HasAdminAccess) return RedirectToAction("AdminLogin");
            var payments = await _context.Payments
                .Join(_context.Bookings, p => p.BookingId, b => b.Id,
                    (payment, booking) => new { payment, booking })
                .Select(x => new Payment
                {
                    Id = x.payment.Id,
                    BookingId = x.payment.BookingId,
                    Amount = x.payment.Amount,
                    Provider = x.payment.Provider,
                    Status = x.payment.Status,
                    TransactionId = x.payment.TransactionId,
                    PaidAt = x.payment.PaidAt
                })
                .ToListAsync();

            ViewBag.Bookings = await _context.Bookings.ToListAsync();
            return View(payments);
        }
    }
}