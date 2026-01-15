namespace Kaya_Otel.Models
{
    public class DeletedTour
    {
        public int Id { get; set; }
        public int OriginalTourId { get; set; } // Orijinal tur ID'si
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public int Capacity { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromHours(4);
        public string ImageUrl { get; set; } = "/images/default-tour.jpg";
        public bool IsActive { get; set; } = true;
        public DateTime DeletedAt { get; set; }
        public string? DeletedBy { get; set; } // Admin kullanıcı adı
    }
}

