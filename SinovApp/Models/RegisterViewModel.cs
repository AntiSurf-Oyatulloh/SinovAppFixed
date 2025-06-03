using System.ComponentModel.DataAnnotations;

namespace SinovApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ismingizni kiriting")]
        [Display(Name = "Ism")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Familiyangizni kiriting")]
        [Display(Name = "Familiya")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email manzilini kiriting")]
        [EmailAddress(ErrorMessage = "To‘g‘ri email manzil kiriting")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Parolni kiriting")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol kamida 6 ta belgidan iborat bo‘lishi kerak")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Parolni tasdiqlang")]
        [Compare("Password", ErrorMessage = "Parollar mos emas")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}