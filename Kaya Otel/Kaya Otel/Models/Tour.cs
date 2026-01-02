namespace Kaya_Otel.Models
{
    public class Tour
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // sunset, mehtap vb.
        public string Description { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public int Capacity { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(4);
        public string ImageUrl { get; set; } = "/images/default-tour.jpg";
        public bool IsActive { get; set; } = true;
    }
}

