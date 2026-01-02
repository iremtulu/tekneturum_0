namespace Kaya_Otel.Models
{
  public class Reservation
  {
    public int Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public bool Available { get; set; }
        public decimal Price { get; set; }
        public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
  }
}
