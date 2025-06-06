using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinovApp.Models
{
    public class Book
    {
        public int Id { get; set; } // Ma'lumotlar bazasi uchun kerak

        [Required(ErrorMessage = "Kitob nomi majburiy")]
        [StringLength(200, ErrorMessage = "Kitob nomi 200 ta belgidan oshmasligi kerak")]
        [Display(Name = "Kitob nomi")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Muallif nomi majburiy")]
        [StringLength(100, ErrorMessage = "Muallif nomi 100 ta belgidan oshmasligi kerak")]
        [Display(Name = "Muallif")]
        public string Author { get; set; } = "";

        [Required(ErrorMessage = "Kategoriya majburiy")]
        [StringLength(50, ErrorMessage = "Kategoriya 50 ta belgidan oshmasligi kerak")]
        [Display(Name = "Kategoriya")]
        public string Category { get; set; } = "";

        // Fayl yo‘li (masalan: /uploads/mybook.pdf)
        [Display(Name = "Fayl yo'li")]
        public string FilePath { get; set; } = "";

        // Yuklanayotgan fayl — bazaga yozilmaydi!
        [NotMapped]
        [Display(Name = "Kitob fayli")]
        public IFormFile? UploadedFile { get; set; }

        // Ixtiyoriy: tashqi link (Google Drive yoki boshqa)
        [Display(Name = "Tashqi havola")]
        public string PdfLink { get; set; } = "";

        [Display(Name = "Fayl nomi")]
        public string RealFileName { get; set; } = ""; // Faylni qurilmaga yuklash uchun asl nom
    }
}