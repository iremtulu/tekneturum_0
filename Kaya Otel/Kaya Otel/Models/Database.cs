using Kaya_Otel.Models;
using System.Collections.Generic;

namespace Kaya_Otel.Models
{
    public static class Database
    {
        public static List<user> Users = new();

        public static List<Tour> Tours = new()
        {
            new Tour
            {
                Id = 1,
                Name = "Özel Kekova Günbatımı Turu",
                Category = "Günbatımı",
                Description = "Gün batımını Kekovanın koylarında karşılayan tekne turu.",
                PricePerPerson = 15000,
                Capacity = 12,
                ImageUrl = "/images/sunset.jpg"
            },
            new Tour
            {
                Id = 2,
                Name = "Mehtap Turu",
                Category = "Mehtap",
                Description = "Gece ışıkları altında Kekovanın sakin sularında  mehtap turu.",
                PricePerPerson = 12000,
                Capacity = 12,
                ImageUrl = "/images/kekova1.jpg"
            },
            new Tour
            {
                Id = 3,
                Name = "Günlük Yemekli Özel Tekne Turu ",
                Category = "Tam Günlük",
                Description = "Koy koy gezerek kekovanın tarihi doğasında akdeniz lezzetlerinin de sizlere eşlik ettiği tur keyfi.",
                PricePerPerson = 20000,
                Capacity = 12,
                Duration = TimeSpan.FromHours(7),
                ImageUrl = "/images/günlükturfoto.jpg"
            }
        };

        public static List<Booking> Bookings = new();
        public static List<Payment> Payments = new();
        public static List<Admin> Admins = new()
        {
            new Admin{ UserName = "admin", Sifre = "123" }
        };
    }
}
