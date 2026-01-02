using Microsoft.AspNetCore.Mvc;
using Kaya_Otel.Models;






namespace Kaya_Otel.Models

{
  public class LoginViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }
}
