using System.Globalization;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Kaya_Otel.Data;
using Kaya_Otel.Models;

namespace Kaya_Otel.Services
{
    public class IyzicoPaymentResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string RawResult { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface IIyzicoPaymentService
    {
        Task<IyzicoPaymentResult> ChargeDepositAsync(Booking booking);
    }

    public class IyzicoPaymentService : IIyzicoPaymentService
    {
        private readonly string _apiKey;
        private readonly string _secretKey;
        private readonly string _baseUrl;
        private readonly ILogger<IyzicoPaymentService> _logger;

        public IyzicoPaymentService(IConfiguration configuration, ILogger<IyzicoPaymentService> logger)
        {
            var section = configuration.GetSection("Payment:Iyzico");
            _apiKey = section["ApiKey"] ?? string.Empty;
            _secretKey = section["SecretKey"] ?? string.Empty;
            _baseUrl = section["BaseUrl"] ?? "https://sandbox-api.iyzipay.com";
            _logger = logger;
        }

        public Task<IyzicoPaymentResult> ChargeDepositAsync(Booking booking)
        {
            return Task.Run(() =>
            {
                var options = new Options
                {
                    ApiKey = _apiKey,
                    SecretKey = _secretKey,
                    BaseUrl = _baseUrl
                };

                var request = new CreatePaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = booking.Id.ToString(),
                    Price = booking.DepositAmount.ToString("0.##", CultureInfo.InvariantCulture),
                    PaidPrice = booking.DepositAmount.ToString("0.##", CultureInfo.InvariantCulture),
                    Currency = Currency.TRY.ToString(),
                    Installment = 1,
                    BasketId = booking.Id.ToString(),
                    PaymentChannel = PaymentChannel.WEB.ToString(),
                    PaymentGroup = PaymentGroup.PRODUCT.ToString()
                };

                // SANDBOX TEST KARTI (kullanıcıdan kart bilgisi alınmadığı için sabit)
                request.PaymentCard = new PaymentCard
                {
                    CardHolderName = booking.CustomerName ?? "Test User",
                    CardNumber = "5528790000000008",
                    ExpireMonth = "12",
                    ExpireYear = "2030",
                    Cvc = "123",
                    RegisterCard = 0
                };

                // Zorunlu alanlar için basit buyer bilgileri
                request.Buyer = new Buyer
                {
                    Id = booking.Id.ToString(),
                    Name = booking.CustomerName ?? "Test",
                    Surname = "Kullanici",
                    GsmNumber = booking.Phone ?? "+905555555555",
                    Email = booking.Email ?? "test@example.com",
                    IdentityNumber = "74300864791",
                    RegistrationDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastLoginDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    RegistrationAddress = "Antalya",
                    City = "Antalya",
                    Country = "Turkey",
                    ZipCode = "07000",
                    Ip = "85.34.78.112"
                };

                var address = new Address
                {
                    ContactName = booking.CustomerName ?? "Test Kullanici",
                    City = "Antalya",
                    Country = "Turkey",
                    Description = "Kekova tekne turu rezervasyonu",
                    ZipCode = "07000"
                };

                request.ShippingAddress = address;
                request.BillingAddress = address;

                var basketItems = new List<BasketItem>
                {
                    new BasketItem
                    {
                        Id = booking.TourId.ToString(),
                        Name = booking.TourName,
                        Category1 = "Tekne Turu",
                        ItemType = BasketItemType.VIRTUAL.ToString(),
                        Price = booking.DepositAmount.ToString("0.##", CultureInfo.InvariantCulture)
                    }
                };

                request.BasketItems = basketItems;

                var iyziPayment = Iyzipay.Model.Payment.Create(request, options);

                _logger.LogInformation("Iyzipay payment response: {@Payment}", iyziPayment);

                var result = new IyzicoPaymentResult
                {
                    Success = string.Equals(iyziPayment.Status, "success", StringComparison.OrdinalIgnoreCase),
                    Status = iyziPayment.Status,
                    TransactionId = iyziPayment.PaymentId ?? string.Empty,
                    RawResult = iyziPayment.ToString() ?? string.Empty,
                    ErrorMessage = iyziPayment.ErrorMessage ?? string.Empty
                };

                return result;
            });
        }
    }
}


