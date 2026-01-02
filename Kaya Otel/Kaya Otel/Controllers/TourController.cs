using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Kaya_Otel.Controllers
{
    public class TourController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TourController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tours = await _context.Tours.Where(t => t.IsActive).ToListAsync();
            return View(tours);
        }

        public async Task<IActionResult> Details(int id)
        {
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.Id == id);
            if (tour == null) return NotFound();
            return View(tour);
        }
    }
}

