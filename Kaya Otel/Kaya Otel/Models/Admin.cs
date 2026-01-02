using System.ComponentModel.DataAnnotations;
namespace Kaya_Otel.Models
{
  public class Admin
  {
    public int Id { get; set; }
    [Required]
    public string UserName { get; set; } = string.Empty;
    [Required]
    [DataType(DataType.Password)]
    public string Sifre { get; set; } = string.Empty;
  }
}
