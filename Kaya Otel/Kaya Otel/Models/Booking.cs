namespace Kaya_Otel.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public int? UserId { get; set; } // Kullanıcı ID'si (opsiyonel - misafir rezervasyonlar için null)
        public string TourName { get; set; } = string.Empty;
        public DateTime TourDate { get; set; }
        public int Guests { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public bool IsDepositPaid { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool CancellationRequested { get; set; } = false;
        public string? CancellationRequestReason { get; set; }
        public DateTime? CancellationRequestedAt { get; set; }
        
        // Rezervasyon Güncelleme Talebi
        public bool UpdateRequested { get; set; } = false;
        public string? UpdateRequestReason { get; set; }
        public DateTime? UpdateRequestedAt { get; set; }
        public string? UpdateRequestStatus { get; set; } // "Pending", "Approved", "Rejected"
        public string? AdminUpdateResponse { get; set; }
        public DateTime? RequestedTourDate { get; set; }
        public int? RequestedGuests { get; set; }
    }
}

