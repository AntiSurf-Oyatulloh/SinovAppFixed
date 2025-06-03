using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace SinovApp.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kitob nomi majburiy maydon")]
        [StringLength(200, ErrorMessage = "Kitob nomi 200 ta belgidan oshmasligi kerak")]
        public string? Title { get; set; } = "";

        [Required(ErrorMessage = "Muallif nomi majburiy maydon")]
        [StringLength(100, ErrorMessage = "Muallif nomi 100 ta belgidan oshmasligi kerak")]
        public string? Author { get; set; } = "";

        [Required(ErrorMessage = "Kategoriya majburiy maydon")]
        [StringLength(50, ErrorMessage = "Kategoriya 50 ta belgidan oshmasligi kerak")]
        public string? Category { get; set; } = "";

        [StringLength(500, ErrorMessage = "Fayl yo'li 500 ta belgidan oshmasligi kerak")]
        public string? FilePath { get; set; } = "";

        [NotMapped]
        [Display(Name = "Kitob fayli")]
        [MaxFileSize(50 * 1024 * 1024, ErrorMessage = "Fayl hajmi 50MB dan oshmasligi kerak")]
        public IFormFile? UploadedFile { get; set; }

        [StringLength(500, ErrorMessage = "Havola 500 ta belgidan oshmasligi kerak")]
        [Url(ErrorMessage = "Noto'g'ri URL format")]
        public string? PdfLink { get; set; }

        [StringLength(255, ErrorMessage = "Fayl nomi 255 ta belgidan oshmasligi kerak")]
        public string? RealFileName { get; set; } = "";

        [StringLength(1000, ErrorMessage = "Tavsif 1000 ta belgidan oshmasligi kerak")]
        public string? Description { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Range(0, int.MaxValue, ErrorMessage = "Yuklanganlar soni manfiy bo'lishi mumkin emas")]
        public int DownloadCount { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Ko'rilganlar soni manfiy bo'lishi mumkin emas")]
        public int ViewCount { get; set; } = 0;

        [StringLength(500, ErrorMessage = "Rasm yo'li 500 ta belgidan oshmasligi kerak")]
        public string? ImagePath { get; set; } = "";

        [NotMapped]
        [Display(Name = "Kitob rasmi")]
        [ImageFile]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Rasm hajmi 5MB dan oshmasligi kerak")]
        public IFormFile? UploadedImage { get; set; }

        public ICollection<LikedBook> LikedByUsers { get; set; } = new List<LikedBook>();
    }

    // Rasm validatsiyasi uchun maxsus atribut
    public class ImageFileAttribute : ValidationAttribute
    {
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!_allowedExtensions.Contains(extension) || !_allowedMimeTypes.Contains(file.ContentType.ToLower()))
                {
                    return new ValidationResult("Faqat rasm formatidagi fayllar yuklanishi mumkin (JPG, PNG, GIF, BMP, WEBP)");
                }
            }
            return ValidationResult.Success;
        }
    }

    // Fayl hajmi uchun validatsiya
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;

        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxFileSize)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}