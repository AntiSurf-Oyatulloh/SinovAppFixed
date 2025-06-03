using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SinovApp.Models;
using SinovApp.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace SinovApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 🏠 Bosh sahifa - barcha kitoblar
        public IActionResult Index()
        {
            var books = _context.Books
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            // Null check qo'shamiz
            if (books != null)
            {
                ViewBag.LatestBooks = books.Take(5).ToList();
                ViewBag.PopularBooks = books.OrderByDescending(b => b.DownloadCount).Take(5).ToList();
                ViewBag.Categories = books.Select(b => b.Category).Distinct().ToList();
            }
            else
            {
                ViewBag.LatestBooks = new List<Book>();
                ViewBag.PopularBooks = new List<Book>();
                ViewBag.Categories = new List<string>();
            }

            return View(books);
        }

        //  Qidiruv funksiyasi
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View("Index", await _context.Books.ToListAsync());

            var filteredBooks = await _context.Books
                .Where(b => (b.Title != null && b.Title.Contains(query)) || 
                           (b.Author != null && b.Author.Contains(query)) || 
                           (b.Category != null && b.Category.Contains(query)))
                .ToListAsync();

            return View("Index", filteredBooks);
        }

        // ⬇️ PDF yuklab olish
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("Fayl nomi ko'rsatilmagan.");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "books", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Fayl topilmadi.");

            var contentType = "application/pdf";
            return File(System.IO.File.ReadAllBytes(filePath), contentType, fileName);
        }

        // ⚙️ Qo'shimcha sahifalar
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}