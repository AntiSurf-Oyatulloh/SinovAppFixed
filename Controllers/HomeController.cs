using Microsoft.AspNetCore.Mvc;
using SinovApp.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace SinovApp.Controllers
{
    public class Book
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Category { get; set; }
        public string? PdfLink { get; set; }
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // O'zgartirish: Modelni yoki ma'lumotlar bazasini qo'shish
        private readonly List<Book> _books;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            // O'zgartirish: Kitoblar ro'yxatini tashqi ma'lumot manbasi bilan o'zgartirishingiz mumkin.
            _books = new List<Book>
            {
                new Book { Title = "Roman 1", Author = "Author 1", Category = "Romanlar", PdfLink = "/books/roman1.pdf" },
                new Book { Title = "Ilmiy kitob 1", Author = "Author 2", Category = "Ilmiy adabiyotlar", PdfLink = "/books/ilmiykitob1.pdf" },
                // Boshqa kitoblar qo'shish
            };
        }

        public IActionResult Index()
        {
            // Indexda barcha kitoblarni ko'rsatamiz
            return View(_books);
        }

        // Qidirish metodini yaratish
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query)) 
            {
                return View("Index", _books); // Agar query null yoki bo'sh bo'lsa, barcha kitoblarni qaytarish
            }

            // Query bo'yicha kitoblarni filtrlash
            var filteredBooks = _books.Where(b => b.Title.Contains(query) || b.Author.Contains(query)).ToList();

            // Natijalarni qaytarish
            return View("Index", filteredBooks);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}