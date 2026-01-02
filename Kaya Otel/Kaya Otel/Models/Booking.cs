namespace Kaya_Otel.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int TourId { get; set; }
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
    }
}

