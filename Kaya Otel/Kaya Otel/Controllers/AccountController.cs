using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Kaya_Otel.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(user user)
        {
            // Kullanıcı adı zaten kayıtlı mı kontrol et
            if (await _context.Users.AnyAsync(u => u.Name == user.Name))
            {
                ModelState.AddModelError("", "Bu kullanıcı adı zaten kayıtlı.");
                return View(user);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Register","Account");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string Name, string password)
        {
            var foundUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Name == Name && u.Password == password);

            if (foundUser == null)
            {
                ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre");
                return View();
            }

            HttpContext.Session.SetString("Name", foundUser.Name);
            ViewBag.Message = "Başarılı giriş";
            return RedirectToAction("Login", "Account");
        }

        
    }
}
