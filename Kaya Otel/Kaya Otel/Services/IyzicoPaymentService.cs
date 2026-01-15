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
        Task<IyzicoPaymentResult> ChargeDepositAsync(Booking booking, string cardHolderName, string cardNumber, string expireMonth, string expireYear, string cvc);
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

        public Task<IyzicoPaymentResult> ChargeDepositAsync(Booking booking, string cardHolderName, string cardNumber, string expireMonth, string expireYear, string cvc)
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

                // Kullanıcıdan alınan kart bilgileri
                request.PaymentCard = new PaymentCard
                {
                    CardHolderName = cardHolderName ?? booking.CustomerName ?? "Test User",
                    CardNumber = cardNumber?.Replace(" ", "").Replace("-", "") ?? "5528790000000008",
                    ExpireMonth = expireMonth ?? "12",
                    ExpireYear = expireYear ?? "2030",
                    Cvc = cvc ?? "123",
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

                // Tüm response'u detaylı loglama
                var fullResponse = iyziPayment.ToString();
                _logger.LogInformation("═══════════════════════════════════════════════════════════");
                _logger.LogInformation("Iyzipay FULL payment response for booking {BookingId}: {FullResponse}", booking.Id, fullResponse);
                _logger.LogInformation("═══════════════════════════════════════════════════════════");
                _logger.LogInformation("Iyzipay payment response details:");
                _logger.LogInformation("  Status: {Status}", iyziPayment.Status);
                _logger.LogInformation("  PaymentStatus: {PaymentStatus} (IsNull: {IsNull})", iyziPayment.PaymentStatus, iyziPayment.PaymentStatus == null);
                _logger.LogInformation("  PaymentId: {PaymentId} (IsNull: {IsNull})", iyziPayment.PaymentId, string.IsNullOrEmpty(iyziPayment.PaymentId));
                _logger.LogInformation("  FraudStatus: {FraudStatus}", iyziPayment.FraudStatus);
                _logger.LogInformation("  ErrorMessage: {ErrorMessage}", iyziPayment.ErrorMessage);
                _logger.LogInformation("  ErrorCode: {ErrorCode}", iyziPayment.ErrorCode);

                // PaymentId kontrolü - Sandbox'ta görünmesi için önemli
                if (!string.IsNullOrEmpty(iyziPayment.PaymentId))
                {
                    _logger.LogInformation("✅ PaymentId oluşturuldu: {PaymentId} - Bu ödeme Iyzico sisteminde kayıtlı!", iyziPayment.PaymentId);
                }
                else
                {
                    _logger.LogWarning("⚠️ PaymentId oluşturulmadı! Ödeme sandbox panosunda görünmeyebilir.");
                }

                // API çağrısı başarılı mı kontrol et
                var isApiSuccess = string.Equals(iyziPayment.Status, "success", StringComparison.OrdinalIgnoreCase);

                // PaymentStatus kontrolü - SUCCESS olmalı, ama null ise PaymentId'ye bak
                var paymentStatus = iyziPayment.PaymentStatus?.ToString() ?? string.Empty;
                var hasPaymentId = !string.IsNullOrEmpty(iyziPayment.PaymentId);

                // PaymentStatus varsa SUCCESS olmalı, yoksa PaymentId varsa başarılı sayılabilir
                // (Iyzico sandbox'ta bazı durumlarda PaymentStatus null olabilir ama PaymentId döner)
                var isPaymentSuccess = false;
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    // PaymentStatus varsa SUCCESS olmalı
                    isPaymentSuccess = string.Equals(paymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                }
                else if (hasPaymentId && isApiSuccess)
                {
                    // PaymentStatus null ama PaymentId varsa ve Status success ise, ödeme başarılı
                    isPaymentSuccess = true;
                    _logger.LogInformation("PaymentStatus is null but PaymentId exists ({PaymentId}) and Status is success. Treating as successful payment.", iyziPayment.PaymentId);
                }
                else if (isApiSuccess && !hasPaymentId)
                {
                    // Status success ama PaymentId yok - bu bir sorun olabilir
                    _logger.LogWarning("Status is success but PaymentId is null for booking {BookingId}. This might indicate an issue.", booking.Id);
                    isPaymentSuccess = false;
                }

                // FraudStatus kontrolü: 1=APPROVED, 2=APPROVED_RISKY, 3=WAITING, 4=REFUSED
                // Sadece 1 ve 2 kabul edilebilir (null ise de kabul edilir - bazı ödeme yöntemlerinde olmayabilir)
                var fraudStatusValue = iyziPayment.FraudStatus?.ToString() ?? string.Empty;
                var isFraudCheckPassed = string.IsNullOrEmpty(fraudStatusValue) ||
                                         fraudStatusValue == "1" ||
                                         fraudStatusValue == "2";

                // Ödeme başarılı sayılması için: API başarılı VE (PaymentStatus SUCCESS veya PaymentId var) VE Fraud kontrolü geçmeli
                var isSuccess = isApiSuccess && isPaymentSuccess && isFraudCheckPassed;

                _logger.LogInformation("Payment validation result for booking {BookingId} - isApiSuccess: {ApiSuccess}, isPaymentSuccess: {PaymentSuccess} (PaymentStatus: {PaymentStatus}, HasPaymentId: {HasPaymentId}), isFraudCheckPassed: {FraudPassed}, Final Success: {Success}",
                    booking.Id, isApiSuccess, isPaymentSuccess, paymentStatus, hasPaymentId, isFraudCheckPassed, isSuccess);

                // Hata mesajını oluştur
                var errorMessage = string.Empty;
                if (!isSuccess)
                {
                    if (!string.IsNullOrEmpty(iyziPayment.ErrorMessage))
                    {
                        // Iyzico'dan gelen hata mesajını kullan
                        errorMessage = iyziPayment.ErrorMessage;
                    }
                    else if (!string.IsNullOrEmpty(iyziPayment.ErrorCode))
                    {
                        // ErrorCode varsa onu kullan
                        errorMessage = $"Ödeme başarısız oldu. Hata kodu: {iyziPayment.ErrorCode}";
                    }
                    else if (!isApiSuccess)
                    {
                        // API çağrısı başarısız
                        errorMessage = $"API çağrısı başarısız oldu. Durum: {iyziPayment.Status}";
                    }
                    else if (!isPaymentSuccess)
                    {
                        // PaymentStatus kontrolü başarısız
                        if (!string.IsNullOrEmpty(paymentStatus))
                        {
                            errorMessage = $"Ödeme başarısız oldu. Durum: {paymentStatus}";
                        }
                        else if (!hasPaymentId)
                        {
                            errorMessage = "Ödeme başarısız oldu. Ödeme ID'si alınamadı.";
                        }
                        else
                        {
                            errorMessage = "Ödeme başarısız oldu.";
                        }
                    }
                    else if (!isFraudCheckPassed)
                    {
                        errorMessage = $"Dolandırıcılık kontrolü başarısız. Durum: {iyziPayment.FraudStatus}";
                    }
                    else
                    {
                        errorMessage = "Ödeme işlemi başarısız oldu. Lütfen kart bilgilerinizi kontrol edip tekrar deneyiniz.";
                    }
                }

                var result = new IyzicoPaymentResult
                {
                    Success = isSuccess,
                    Status = iyziPayment.Status ?? string.Empty,
                    TransactionId = iyziPayment.PaymentId ?? string.Empty,
                    RawResult = iyziPayment.ToString() ?? string.Empty,
                    ErrorMessage = errorMessage
                };

                return result;
            });
        }
    }
}


