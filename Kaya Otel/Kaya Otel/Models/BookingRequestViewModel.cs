using System.ComponentModel.DataAnnotations;

namespace Kaya_Otel.Models
{
    public class BookingRequestViewModel
    {
        [Required]
        public int TourId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime TourDate { get; set; }

        [Required]
        [Range(1, 50)]
        public int Guests { get; set; }

        [Required]
        [StringLength(60)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Telefon numarası tam olarak 11 haneli olmalıdır.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Telefon numarası sadece rakamlardan oluşmalıdır.")]
        public string Phone { get; set; } = string.Empty;
    }
}

