namespace Kaya_Otel.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, warning, success, danger
        public int? UserId { get; set; } // Kullanıcı ID'si (null ise admin'e gider)
        public int? AdminId { get; set; } // Admin ID'si (null ise kullanıcıya gider)
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? RelatedBookingId { get; set; } // İlgili rezervasyon ID'si
        public string? CancellationReason { get; set; } // İptal nedeni
    }
}

