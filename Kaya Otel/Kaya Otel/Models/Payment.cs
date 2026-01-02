namespace Kaya_Otel.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Provider { get; set; } = "iyzico";
        public string Status { get; set; } = "Pending";
        public string TransactionId { get; set; } = string.Empty;
        public DateTime PaidAt { get; set; }
    }
}

