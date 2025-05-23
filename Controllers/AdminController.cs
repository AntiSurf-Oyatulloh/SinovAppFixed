using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinovApp.Models;
using SinovApp.Data;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace SinovApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];
            return View(await _context.Books.ToListAsync());
        }

        [HttpGet]
        public IActionResult AddBook() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBook(Book newBook)
        {
            if (!ModelState.IsValid)
                return View(newBook);

            // 🔁 Kitob nomi bo'yicha takroran qo'shilmasligi uchun tekshiruv
            if (await _context.Books.AnyAsync(b => b.Title.ToLower() == newBook.Title.ToLower()))
            {
                ModelState.AddModelError("", "❗ Bu nomdagi kitob allaqachon mavjud.");
                return View(newBook);
            }

            // 📥 Fayl yuklangan bo'lsa – serverga saqlanadi
            if (newBook.UploadedFile != null && newBook.UploadedFile.Length > 0)
            {
                var fileName = Path.GetFileName(newBook.UploadedFile.FileName);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "books");

                // 🎯 Agar "books" papkasi mavjud bo'lmasa – yaratiladi
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newBook.UploadedFile.CopyToAsync(stream);
                }

                // 🧭 Faylga havola yaratib qo'yiladi
                newBook.FilePath = "/books/" + fileName;
            }

            // 🌐 Tashqi havola ko'rsatilgan bo'lsa va fayl yuklanmagan bo'lsa
            if (!string.IsNullOrEmpty(newBook.PdfLink) && string.IsNullOrEmpty(newBook.FilePath))
            {
                newBook.FilePath = newBook.PdfLink;
            }

            _context.Books.Add(newBook);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "✅ Yangi kitob muvaffaqiyatli qo'shildi!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBook(string title)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Title == title);
            if (book != null)
            {
                // ❌ Fayl manzili "/books/" bilan boshlansa – uni fizik diskdan o'chiramiz
                if (!string.IsNullOrEmpty(book.FilePath) && book.FilePath.StartsWith("/books/"))
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.FilePath.TrimStart('/'));
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
                DarslikCount = await _context.Books.CountAsync(b => b.Category == "Darsliklar")
            };

            return View(model);
        }
    }
}