using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SinovApp.Models
{
    // Identity foydalanuvchi modelini kengaytirish
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "To‘liq ism majburiy")]
        [StringLength(100, ErrorMessage = "Ism juda uzun")]
        public string? FullName { get; set; } = string.Empty;
    }
}