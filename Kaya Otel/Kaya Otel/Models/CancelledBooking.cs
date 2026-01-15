namespace Kaya_Otel.Models
{
    public class CancelledBooking
    {
        public int Id { get; set; }
        public int OriginalBookingId { get; set; } // Orijinal rezervasyon ID'si
        public int TourId { get; set; }
        public string TourName { get; set; } = string.Empty;
        public DateTime TourDate { get; set; }
        public int Guests { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? UserId { get; set; } // Kullanıcı ID'si (opsiyonel - misafir rezervasyonlar için null)
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public bool IsDepositPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
        public string? CancelledBy { get; set; } // Admin kullanıcı adı
        public string? CancellationReason { get; set; }
    }
}

