using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SinovApp.Models
{
    // Identity foydalanuvchi modelini kengaytirish
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "To‘liq ism majburiy")]
        [StringLength(100, ErrorMessage = "Ism juda uzun")]
        public string? FullName { get; set; } = string.Empty;

        // Profil rasmi yo'li (masalan, /images/profile/user_id.jpg)
        public string? ImagePath { get; set; }

        // Foydalanuvchi haqida qisqa ma'lumot
        [StringLength(500, ErrorMessage = "Bio juda uzun")]
        public string? Bio { get; set; }

        // Yaxshi ko'rgan kitoblar
        public ICollection<LikedBook> LikedBooks { get; set; } = new List<LikedBook>();

        // Foydalanuvchining kitoblari bilan bog'liqlik (Agar mavjud bo'lsa)
        // public ICollection<UserBook>? UserBooks { get; set; } // Misol uchun, agar UserBook modeli bo'lsa

        // Ro'yxatdan o'tgan sana (Agar IdentityUserda default bo'lmasa yoki aniq kerak bo'lsa)
        // public DateTime CreatedAt { get; set; }
    }
}