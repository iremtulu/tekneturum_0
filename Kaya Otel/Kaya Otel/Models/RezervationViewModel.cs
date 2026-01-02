
using System;
using System.Collections.Generic;



namespace Kaya_Otel.Models
{
  public class RezervationViewModel
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Childeren { get; set; }
    public int Adult { get; set; }
    public double Price { get; set; }

  }
}
