using System.Text.Json.Serialization;

namespace Kaya_Otel.Models
{
  public class user
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [JsonIgnore] // JSON serialization'da password alanını gizle
    public string Password { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
}
