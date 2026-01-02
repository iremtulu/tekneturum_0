using System;
using System.Collections.Generic;


namespace Kaya_Otel.Models
{
  public class SerachViewModel
  {
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Children { get; set; }
    public int Adults { get; set; }
    public List<Room> Rooms { get; set; } = new List<Room>();
  }
}
