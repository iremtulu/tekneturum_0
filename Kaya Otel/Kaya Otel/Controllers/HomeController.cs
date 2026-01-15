using Kaya_Otel.Data;
using Kaya_Otel.Models;
using Kaya_Otel.Services;
using Kaya_Otel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Kaya_Otel.Controllers

{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IInstagramService _instagramService;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, IInstagramService instagramService, ApplicationDbContext context)
    {
      _logger = logger;
      _instagramService = instagramService;
      _context = context;
    }

    public async Task<IActionResult> Index()
    {
      // Ana sayfa her zaman gösterilsin (giriş yapmış olsa bile)
      // Admin veya kullanıcı giriş yapmışsa normal ana sayfayı göster, panel'e yönlendirme yapma
      var tours = await _context.Tours.Where(t => t.IsActive).ToListAsync();
      var posts = await _instagramService.GetLatestPostsAsync();
      ViewBag.HeroTittle = "Günübirlik Tekne Turları";
      ViewBag.Tours = tours;
      ViewBag.FeaturedTour = tours.FirstOrDefault();
      ViewBag.InstagramPosts = posts;
      return View();
    }
    public IActionResult Contact()
    {
      var c = new ContactM
      {
        Adres = "Konyaaltı/Antalya",
      PhoneNumber = "05395603136",
      Email = "abc123@gmail.com"

    };
    return View(c);
    }
    [HttpGet]
    public async Task<IActionResult> Instagram()
    {
      IReadOnlyList<InstagramPostViewModel> posts = await _instagramService.GetLatestPostsAsync();
      return PartialView("~/Views/Shared/_InstagramPosts.cshtml", posts);
    }
    public IActionResult Privacy()
    {
      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    public IActionResult CheckSession()
    {
      var isAdmin = HttpContext.Session.GetString("AdminGirisi") == "true";
      var isUser = HttpContext.Session.GetString("UserGirisi") == "true";
      
      return Json(new
      {
        isAdmin = isAdmin,
        isUser = isUser,
        adminName = isAdmin ? HttpContext.Session.GetString("AdminUserName") : null,
        userName = isUser ? HttpContext.Session.GetString("UserName") : null
      });
    }
  }
}
