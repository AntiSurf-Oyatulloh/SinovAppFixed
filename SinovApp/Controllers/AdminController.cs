using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinovApp.Models;
using SinovApp.Data;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinovApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<AdminController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Admin/Index action accessed");
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];
            return View(await _context.Books.OrderByDescending(b => b.CreatedAt).ToListAsync());
        }

        [HttpGet]
        public IActionResult AddBook() => View();

        private bool IsValidFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("File is null or empty");
                    return false;
                }

                var extension = Path.GetExtension(file.FileName)?.ToLower();
                _logger.LogInformation($"File extension: {extension}");
                _logger.LogInformation($"Content type: {file.ContentType}");

                // Ruxsat etilgan fayl kengaytmalari
                var allowedExtensions = new[] {
                    ".pdf", ".doc", ".docx", ".txt", ".rtf",
                    ".xls", ".xlsx", ".csv",
                    ".ppt", ".pptx",
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
                    ".zip", ".rar", ".7z"
                };

                // Xavfli fayl kengaytmalari (bloklanadigan)
                var blockedExtensions = new[] {
                    ".exe", ".dll", ".js", ".vbs", ".bat", ".cmd",
                    ".ps1", ".sh", ".php", ".asp", ".aspx", ".jsp",
                    ".html", ".htm", ".xml", ".json", ".sql" // Xavfli bo'lishi mumkin bo'lgan boshqalar
                };

                // Kengaytma null yoki bo'sh bo'lsa yoki ruxsat etilganlar ro'yxatida bo'lmasa
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Unsupported file extension: {extension}");
                    return false;
                }

                // Kengaytma bloklanganlar ro'yxatida bo'lsa
                if (blockedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"Blocked file extension: {extension}");
                    return false;
                }

                // Max. fayl hajmini tekshirish (50MB)
                if (file.Length > 50 * 1024 * 1024)
                {
                    _logger.LogWarning($"File size exceeds 50MB limit: {file.Length} bytes");
                    return false;
                }

                // Agar PDF fayl bo'lsa, qo'shimcha imzo tekshiruvini qilamiz (EOF tekshiruvisiz)
                if (extension == ".pdf")
                {
                    using (var stream = file.OpenReadStream())
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            // Read first 8 bytes to check PDF signature
                            if (file.Length < 8) // Fayl juda qisqa bo'lsa, imzo bo'lmaydi
                            {
                                _logger.LogWarning("File too short for PDF signature check.");
                                return false;
                            }

                            var header = reader.ReadBytes(8);
                            var pdfSignature = System.Text.Encoding.ASCII.GetString(header);

                            _logger.LogInformation($"PDF signature found: {pdfSignature}");

                            // Check for PDF signature (both %PDF- and %PDF+)
                            if (!pdfSignature.StartsWith("%PDF-") && !pdfSignature.StartsWith("%PDF+"))
                            {
                                _logger.LogWarning($"Invalid PDF signature: {pdfSignature}");
                                return false;
                            }
                        }
                    }
                }

                // Boshqa fayl turlari uchun ContentType tekshiruvi (PDF uchun imzo tekshiruvidan keyin)
                // Qat'iy ContentType tekshiruvini qo'shish mumkin, lekin ba'zan u ishonchsiz bo'lishi mumkin.
                // Hozircha faqat kengaytma va PDF imzosini tekshirishga tayanaman.

                _logger.LogInformation("File validation successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file");
                return false;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(Book newBook)
        {
            // Diagnostika loglari olib tashlandi
            // if (string.IsNullOrEmpty(newBook.Category))
            // {
            //     ModelState.AddModelError("Category", "###DEBUG_CATEGORY_BINDING_FAILED### Kategoriya qiymati serverga kelganda bo'sh yoki null.");
            //     _logger.LogWarning("###DEBUG_CATEGORY_BINDING_FAILED### newBook.Category is null or empty on arrival.");
            // }
            // else
            // {
            //      _logger.LogInformation($"###DEBUG_CATEGORY_VALUE### Kategoriya qiymati serverga keldi: '{newBook.Category}' (Uzunligi: {newBook.Category.Length})");
            // }
            // _logger.LogWarning($"###CATEGORY_DEBUG### ModelState.IsValid early: {ModelState.IsValid}");

            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");

            // Tavsif, PdfLink va Category maydonlarini ixtiyoriy qilish
            ModelState.Remove("Description");
            ModelState.Remove("PdfLink");
            // ModelState.Remove("Category"); // Kategoriya validatsiyasini o'chirmaymiz

            if (!ModelState.IsValid)
            {
                // ModelState error loglari olib tashlandi
                // foreach (var modelStateEntry in ModelState.Where(e => e.Value.Errors.Any()))
                // {
                //     var propertyName = modelStateEntry.Key;
                //     var errors = modelStateEntry.Value.Errors;
                //     foreach (var error in errors)
                //     {
                //         _logger.LogError($"###MODELSTATE_ERROR_DEBUG### Property: {propertyName}, Error: {error.ErrorMessage}");
                //     }
                // }
                return View(newBook);
            }

            // 🔁 Kitob nomi bo'yicha takroran qo'shilmasligi uchun tekshiruv
            if (newBook.Title != null && await _context.Books.AnyAsync(b => b.Title != null && b.Title.ToLower() == newBook.Title.ToLower()))
            {
                ModelState.AddModelError("", "❗ Bu nomdagi kitob allaqachon mavjud.");
                return View(newBook);
            }

            string? savedFilePath = null;
            string? savedFileName = null;

            // 📥 Fayl yuklangan bo'lsa – serverga saqlanadi
            if (newBook.UploadedFile != null && newBook.UploadedFile.Length > 0)
            {
                _logger.LogInformation($"###UPLOAD_DEBUG### Fayl yuklash boshlandi: {newBook.UploadedFile.FileName}");
                _logger.LogInformation($"###UPLOAD_DEBUG### Fayl hajmi: {newBook.UploadedFile.Length}");
                _logger.LogInformation($"###UPLOAD_DEBUG### Content type: {newBook.UploadedFile.ContentType}");

                // Validate file
                _logger.LogInformation("###UPLOAD_DEBUG### Fayl validatsiyasidan o'tish tekshirilmoqda...");
                if (!IsValidFile(newBook.UploadedFile))
                {
                    _logger.LogWarning("###UPLOAD_DEBUG### Fayl validatsiyadan o'tmadi.");
                    ModelState.AddModelError("UploadedFile", "❌ Noto'g'ri fayl turi yoki xavfli fayl!");
                    return View(newBook);
                }
                _logger.LogInformation("###UPLOAD_DEBUG### Fayl validatsiyadan o'tdi.");

                try
                {
                    // Fayl nomini xavfsizlashtirish
                    var safeFileName = Path.GetFileNameWithoutExtension(newBook.UploadedFile.FileName)
                        .Replace(" ", "_")
                        .Replace("'", "")
                        .Replace("\"", "")
                        .Replace("&", "")
                        .Replace("?", "")
                        .Replace("#", "")
                        .Replace("%", "")
                        .Replace("+", "")
                        .Replace("=", "")
                        .Replace("\\", "")
                        .Replace("/", "")
                        .Replace(":", "")
                        .Replace("*", "")
                        .Replace("<", "")
                        .Replace(">", "")
                        .Replace("|", "");

                    var extension = Path.GetExtension(newBook.UploadedFile.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid()}_{safeFileName}{extension}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "books");
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    _logger.LogInformation($"Uploads folder: {uploadsFolder}");
                    _logger.LogInformation($"Full file path: {filePath}");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        _logger.LogInformation($"Creating directory: {uploadsFolder}");
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Save file
                    _logger.LogInformation($"Saving file to: {filePath}");
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await newBook.UploadedFile.CopyToAsync(stream);
                        _logger.LogInformation("File saved successfully");
                    }

                    // Verify file was saved
                    if (System.IO.File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        _logger.LogInformation($"File exists after save: {filePath}");
                        _logger.LogInformation($"File size after save: {fileInfo.Length} bytes");

                        savedFilePath = "/books/" + fileName;
                        savedFileName = newBook.UploadedFile.FileName;
                    }
                    else
                    {
                        _logger.LogError($"File was not saved successfully: {filePath}");
                        ModelState.AddModelError("UploadedFile", "Fayl yuklashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                        return View(newBook);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fayl yuklashda xatolik yuz berdi");
                    ModelState.AddModelError("UploadedFile", "Fayl yuklashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                    return View(newBook);
                }
            }
            // 🌐 Tashqi havola ko'rsatilgan bo'lsa va fayl yuklanmagan bo'lsa
            else if (!string.IsNullOrEmpty(newBook.PdfLink))
            {
                savedFilePath = newBook.PdfLink;
            }
            else
            {
                ModelState.AddModelError("", "❗ Iltimos, fayl yuklang yoki tashqi havolani kiriting.");
                return View(newBook);
            }

            // 📸 Rasm yuklangan bo'lsa
            if (newBook.UploadedImage != null && newBook.UploadedImage.Length > 0)
            {
                // Rasm turini tekshirish
                var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
                _logger.LogInformation($"Uploaded image content type: {newBook.UploadedImage.ContentType}");
                _logger.LogInformation($"Uploaded image file name: {newBook.UploadedImage.FileName}");
                _logger.LogInformation($"Uploaded image length: {newBook.UploadedImage.Length}");

                if (!allowedImageTypes.Contains(newBook.UploadedImage.ContentType.ToLower()))
                {
                    _logger.LogWarning($"Invalid image type: {newBook.UploadedImage.ContentType}");
                    ModelState.AddModelError("UploadedImage", "Faqat rasm formatidagi fayllar yuklanishi mumkin (JPG, PNG, GIF, BMP, WEBP)");
                    return View(newBook);
                }

                // Rasm hajmini tekshirish (5MB)
                if (newBook.UploadedImage.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("UploadedImage", "Rasm hajmi 5MB dan oshmasligi kerak");
                    return View(newBook);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(newBook.UploadedImage.FileName)}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "books");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await newBook.UploadedImage.CopyToAsync(stream);
                    }

                    newBook.ImagePath = "/images/books/" + fileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rasm yuklashda xatolik yuz berdi");
                    ModelState.AddModelError("UploadedImage", "Rasm yuklashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                    return View(newBook);
                }
            }

            try
            {
                // Set book properties
                newBook.FilePath = savedFilePath;
                newBook.RealFileName = savedFileName;
                newBook.CreatedAt = DateTime.Now;

                // Save to database
                _context.Books.Add(newBook);
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Yangi kitob muvaffaqiyatli qo'shildi!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database save error");

                // Clean up uploaded file if database save fails
                if (!string.IsNullOrEmpty(savedFilePath) && savedFilePath.StartsWith("/books/"))
                {
                    var physicalPath = Path.Combine(_environment.WebRootPath, savedFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        try
                        {
                            System.IO.File.Delete(physicalPath);
                            _logger.LogInformation($"Cleaned up file after database error: {physicalPath}");
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogError(deleteEx, $"Failed to clean up file after database error: {physicalPath}");
                        }
                    }
                }

                ModelState.AddModelError("", "❌ Kitobni saqlashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                return View(newBook);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditBook(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                if (book == null)
                {
                    TempData["Error"] = "❌ Kitob topilmadi.";
                    return RedirectToAction(nameof(Index));
                }

                return View(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditBook GET actionida xatolik yuz berdi");
                TempData["Error"] = "❌ Kitobni yuklashda xatolik yuz berdi.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBook(Book book)
        {
            try
            {
                // Tavsif, PdfLink va Category maydonlarini ixtiyoriy qilish
                ModelState.Remove("Description");
                ModelState.Remove("PdfLink");
                ModelState.Remove("Category");

                if (!ModelState.IsValid)
                {
                    _logger.LogError($"EditBook POST: Model state invalid.");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogError($"EditBook POST ModelState error: {error.ErrorMessage}");
                    }
                    return View(book);
                }

                var existingBook = await _context.Books.FindAsync(book.Id);
                if (existingBook == null)
                {
                    TempData["Error"] = "❌ Kitob topilmadi.";
                    return RedirectToAction(nameof(Index));
                }

                // 📥 Yangi fayl yuklangan bo'lsa
                if (book.UploadedFile != null && book.UploadedFile.Length > 0)
                {
                    // Validate file
                    if (!IsValidFile(book.UploadedFile))
                    {
                        ModelState.AddModelError("UploadedFile", "❌ Noto'g'ri fayl turi yoki xavfli fayl!");
                        return View(book);
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(book.UploadedFile.FileName)}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "books");

                    // Agar "books" papkasi mavjud bo'lmasa – yaratiladi
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    try
                    {
                        // Eski faylni o'chirish
                        if (!string.IsNullOrEmpty(existingBook.FilePath) && existingBook.FilePath.StartsWith("/books/"))
                        {
                            var oldFilePath = Path.Combine(_environment.WebRootPath, existingBook.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await book.UploadedFile.CopyToAsync(stream);
                        }

                        existingBook.FilePath = "/books/" + fileName;
                        existingBook.RealFileName = book.UploadedFile.FileName;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fayl yuklashda xatolik yuz berdi");
                        ModelState.AddModelError("UploadedFile", "Fayl yuklashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                        return View(book);
                    }
                }

                // 📸 Yangi rasm yuklangan bo'lsa
                if (book.UploadedImage != null && book.UploadedImage.Length > 0)
                {
                    // Rasm turini tekshirish
                    var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
                    _logger.LogInformation($"Uploaded image content type: {book.UploadedImage.ContentType}");
                    _logger.LogInformation($"Uploaded image file name: {book.UploadedImage.FileName}");
                    _logger.LogInformation($"Uploaded image length: {book.UploadedImage.Length}");

                    if (!allowedImageTypes.Contains(book.UploadedImage.ContentType.ToLower()))
                    {
                        _logger.LogWarning($"Invalid image type: {book.UploadedImage.ContentType}");
                        ModelState.AddModelError("UploadedImage", "Faqat rasm formatidagi fayllar yuklanishi mumkin (JPG, PNG, GIF, BMP, WEBP)");
                        return View(book);
                    }

                    // Rasm hajmini tekshirish (5MB)
                    if (book.UploadedImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("UploadedImage", "Rasm hajmi 5MB dan oshmasligi kerak");
                        return View(book);
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(book.UploadedImage.FileName)}";
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "books");

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);

                    try
                    {
                        // Eski rasmini o'chirish
                        if (!string.IsNullOrEmpty(existingBook.ImagePath) && existingBook.ImagePath.StartsWith("/images/books/"))
                        {
                            var oldImagePath = Path.Combine(_environment.WebRootPath, existingBook.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await book.UploadedImage.CopyToAsync(stream);
                        }

                        existingBook.ImagePath = "/images/books/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Rasm yuklashda xatolik yuz berdi");
                        ModelState.AddModelError("UploadedImage", "Rasm yuklashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                        return View(book);
                    }
                }

                // 🌐 Tashqi havola ko'rsatilgan bo'lsa va fayl yuklanmagan bo'lsa
                if (!string.IsNullOrEmpty(book.PdfLink) && string.IsNullOrEmpty(existingBook.FilePath))
                {
                    existingBook.FilePath = book.PdfLink;
                }

                // Kitob ma'lumotlarini yangilash
                existingBook.Title = book.Title;
                existingBook.Author = book.Author;
                existingBook.Category = book.Category;
                existingBook.Description = book.Description;
                existingBook.PdfLink = book.PdfLink;

                // Bazaga saqlash
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Kitob muvaffaqiyatli yangilandi!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditBook POST actionida xatolik yuz berdi");
                ModelState.AddModelError("", "❌ Kitobni yangilashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                return View(book);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                // ❌ Fayl manzili "/books/" bilan boshlansa – uni fizik diskdan o'chiramiz
                if (!string.IsNullOrEmpty(book.FilePath) && book.FilePath.StartsWith("/books/"))
                {
                    var physicalPath = Path.Combine(_environment.WebRootPath, book.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }

                // ❌ Rasm manzili "/images/books/" bilan boshlansa – uni fizik diskdan o'chiramiz
                if (!string.IsNullOrEmpty(book.ImagePath) && book.ImagePath.StartsWith("/images/books/"))
                {
                    var physicalPath = Path.Combine(_environment.WebRootPath, book.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                TempData["Success"] = "🗑️ Kitob muvaffaqiyatli o'chirildi.";
            }
            else
            {
                TempData["Error"] = "❌ Kitob topilmadi.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Statistics()
        {
            var model = new
            {
                TotalBooks = await _context.Books.CountAsync(),
                RomanCount = await _context.Books.CountAsync(b => b.Category == "Romanlar"),
                IlmiyCount = await _context.Books.CountAsync(b => b.Category == "Ilmiy adabiyotlar"),
                DarslikCount = await _context.Books.CountAsync(b => b.Category == "Darsliklar"),
                TotalDownloads = await _context.Books.SumAsync(b => b.DownloadCount),
                TotalViews = await _context.Books.SumAsync(b => b.ViewCount),
                RecentBooks = await _context.Books
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}