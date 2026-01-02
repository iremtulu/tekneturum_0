using Kaya_Otel.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kaya_Otel.Controllers
{
  public class UserController:Controller
  {
    [HttpGet]
    public IActionResult Login()
    {
      return View();
    }
    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
      if (ModelState.IsValid)
      {
        if (model.Name=="irem" && model.Password=="123") {
          RedirectToAction("Index", "Home");

}
        ViewBag.Error = "Hatalı Giriş";
      }
      return View(model);

    }


  }
}
