using System.ComponentModel.DataAnnotations;
namespace Kaya_Otel.Models
{
  public class Admin
  {
    public int Id { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty; // Geriye dönük uyumluluk için
    [Required]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
}
