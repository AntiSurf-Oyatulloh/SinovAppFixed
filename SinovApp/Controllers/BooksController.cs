using Microsoft.AspNetCore.Mvc;
using SinovApp.Data;
using SinovApp.Models;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace SinovApp.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BooksController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public BooksController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager,
            ILogger<BooksController> logger)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Romans()
        {
            var romans = _context.Books.Where(b => b.Category == "Romanlar").ToList();
            return View("Romans", romans);
        }

        public IActionResult Science()
            {
            var science = _context.Books.Where(b => b.Category == "Ilmiy adabiyotlar").ToList();
            return View("Science", science);
        }

        public IActionResult Textbooks()
        {
            var textbooks = _context.Books.Where(b => b.Category == "Darsliklar").ToList();
            return View("Textbooks", textbooks);
            }

        // 📥 Fayl yuklash
        public async Task<IActionResult> Download(string fileName, bool isPreview = false)
        {
            _logger.LogInformation($"Download action called for fileName: {fileName}, isPreview: {isPreview}");

            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Download: File name is null or empty.");
                return BadRequest("Fayl nomi ko'rsatilmagan.");
            }

            var filePath = Path.Combine(_environment.WebRootPath, "books", fileName);
            _logger.LogInformation($"Download: Constructed file path: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning($"Download: File not found at path: {filePath}");
                return NotFound("Fayl topilmadi.");
            }

            // Kitobni topamiz va yuklanishlar sonini oshiramiz (agar preview bo'lmasa)
            if (!isPreview)
            {
                var book = await _context.Books.FirstOrDefaultAsync(b => b.FilePath != null && b.FilePath.EndsWith(fileName));
                if (book != null)
                {
                    book.DownloadCount++;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Download: Download count incremented for book ID: {book.Id}");
                }
                else
                {
                     _logger.LogWarning($"Download: Book not found in DB for fileName: {fileName} for download count.");
        }
            }

            var contentType = "application/octet-stream";
            var originalFileName = fileName; // Default to file name

            // Agar kitob topilsa, asl fayl nomini olamiz
            var bookForFileName = await _context.Books.FirstOrDefaultAsync(b => b.FilePath != null && b.FilePath.EndsWith(fileName));
            if (bookForFileName != null && !string.IsNullOrEmpty(bookForFileName.RealFileName))
            {
                originalFileName = bookForFileName.RealFileName;
                _logger.LogInformation($"Download: Using original file name from DB: {originalFileName}");
            }

            var fileExtension = Path.GetExtension(fileName)?.ToLower();

            if (isPreview && fileExtension == ".pdf")
            {
                 _logger.LogInformation($"Download: Serving PDF inline for preview: {originalFileName}");
                // PDF fayllar uchun inline ko'rsatish
                return PhysicalFile(filePath, "application/pdf"); // Original file name not needed for inline
            }
            else
            {
                 _logger.LogInformation($"Download: Serving file for download: {originalFileName}");
                // Boshqa fayllar yoki yuklab olish uchun attachment
                return PhysicalFile(filePath, contentType, originalFileName);
            }
        }

        // Kitob faylini ko'rish uchun endpoint
        public async Task<IActionResult> Preview(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            // Ko'rishlar sonini oshiramiz
            book.ViewCount++;
            await _context.SaveChangesAsync();

            // Agar tashqi havola bo'lsa, o'shanga yo'naltiramiz (yoki modal ichida ko'rsatish uchun URLni qaytaramiz)
            if (!string.IsNullOrEmpty(book.PdfLink))
        {
                // Agar tashqi havola https bilan boshlansa, to'g'ridan-to'g'ri qaytaramiz
                if (Uri.TryCreate(book.PdfLink, UriKind.Absolute, out Uri? externalUri) && (externalUri.Scheme == Uri.UriSchemeHttp || externalUri.Scheme == Uri.UriSchemeHttps))
            {
                    return Content(book.PdfLink, "text/plain"); // JavaScript bu URLni olib iframe src ga joylashtiradi
                }
                else
                {
                    // Agar havola yaroqsiz bo'lsa
                    _logger.LogWarning($"###PREVIEW_DEBUG### Invalid PdfLink format for book ID {id}: {book.PdfLink}");
                    return StatusCode(StatusCodes.Status400BadRequest, "Yaroqsiz tashqi havola format");
                }
            }

            // Agar fayl bo'lsa, uni qaytaramiz
            if (!string.IsNullOrEmpty(book.FilePath))
            {
                // FilePath allaqachon wwwroot/books/ ichidagi to'liq yo'lni saqlaydi (yoki shunday bo'lishi kerak)
                var relativePath = book.FilePath.TrimStart('/', '\\');
                var filePath = Path.Combine(_environment.WebRootPath, relativePath); // Directory.GetCurrentDirectory() o'rniga _environment.WebRootPath ishlatildi

                // Xavfsizlik tekshiruvi: fayl uploads papkasi ichida ekanligiga ishonch hosil qilish
                var wwwrootFolder = _environment.WebRootPath + Path.DirectorySeparatorChar; // wwwroot yo'li
                // var expectedUploadsFolder = Path.Combine(wwwrootFolder, "books" + Path.DirectorySeparatorChar);
                
                if (!filePath.StartsWith(wwwrootFolder, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"###PREVIEW_DEBUG### Attempted to access file outside wwwroot: {filePath}");
                    return StatusCode(StatusCodes.Status403Forbidden, "Faylga kirish ruxsat etilmagan");
                }
                
                // Yo'l wwwroot/books ichida ekanligiga ishonch hosil qilish uchun qo'shimcha tekshiruv (ixtiyoriy, lekin xavfsizroq)
                if (!filePath.StartsWith(Path.Combine(_environment.WebRootPath, "books"), StringComparison.OrdinalIgnoreCase))
                {
                     _logger.LogWarning($"###PREVIEW_DEBUG### Attempted to access file outside books folder: {filePath}");
                     return StatusCode(StatusCodes.Status403Forbidden, "Faylga kirish ruxsat etilmagan: Kitoblar papkasida emas.");
                }

            if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"###PREVIEW_DEBUG### File not found on server: {filePath}");
                    return NotFound("Kitob fayli serverda topilmadi.");
                }

                try
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    // var contentType = "application/pdf"; // Fayl turini aniqlash kerak bo'lishi mumkin
                    
                    // Fayl kengaytmasiga qarab ContentType ni aniqlash
                    var fileExtension = Path.GetExtension(filePath)?.ToLowerInvariant();
                    var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                    if (!contentTypeProvider.TryGetContentType(filePath, out var contentType))
                    {
                         contentType = "application/octet-stream"; // Agar aniqlanmasa, umumiy turni ishlatish
                    }
                    
                    _logger.LogInformation($"###PREVIEW_DEBUG### Serving file for preview: {filePath} with ContentType: {contentType}");
                    return File(fileBytes, contentType); // Faylni kontent sifatida qaytaramiz
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(ioEx, $"###PREVIEW_DEBUG### IO error reading file for book ID {id}: {filePath}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Faylni o'qishda server xatosi");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"###PREVIEW_DEBUG### Unexpected error reading file for book ID {id}: {filePath}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Faylni tayyorlashda server xatosi");
            }
            }

            _logger.LogWarning($"###PREVIEW_DEBUG### No FilePath or PdfLink found for book ID: {id}");
            return NotFound("Ko'rish uchun fayl yoki havola topilmadi.");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var existingLike = await _context.LikedBooks
                .FirstOrDefaultAsync(l => l.BookId == id && l.UserId == user.Id);

            if (existingLike != null)
            {
                _context.LikedBooks.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Json(new { liked = false });
            }
            else
            {
                var newLike = new LikedBook
                {
                    BookId = id,
                    UserId = user.Id,
                    LikedAt = DateTime.Now
                };
                _context.LikedBooks.Add(newLike);
                await _context.SaveChangesAsync();
                return Json(new { liked = true });
            }
        }
    }
}