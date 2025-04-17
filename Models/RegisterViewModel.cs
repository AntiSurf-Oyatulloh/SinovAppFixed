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
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Parolni kiriting")]
        [DataType(DataType.Password)]
        [Display(Name = "Parol")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol kamida 6 ta belgidan iborat bo‘lishi kerak")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Parolni tasdiqlang")]
        [DataType(DataType.Password)]
        [Display(Name = "Parolni tasdiqlash")]
        [Compare("Password", ErrorMessage = "Parollar mos kelmayapti")]
        public string ConfirmPassword { get; set; } = "";
    }
}