using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinovApp.Models;

namespace SinovApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminController : Controller
    {
        // ⚠️ Test maqsadida vaqtincha static ro'yxat, aslida bu DB bo'lishi kerak
        private static List<Book> _books = new()
        {
            new Book { Title = "Roman 1", Author = "Muallif 1", Category = "Romanlar", PdfLink = "/books/roman1.pdf" },
            new Book { Title = "Ilmiy kitob 1", Author = "Muallif 2", Category = "Ilmiy adabiyotlar", PdfLink = "/books/ilmiykitob1.pdf" }
        };

        // GET: /Admin
        public IActionResult Index()
        {
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];
            return View(_books);
        }

        // GET: /Admin/AddBook
        [HttpGet]
        public IActionResult AddBook()
        {
            return View();
        }

        // POST: /Admin/AddBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBook(Book newBook)
        {
            if (!ModelState.IsValid)
                return View(newBook);

            if (_books.Any(b => b.Title == newBook.Title))
            {
                ModelState.AddModelError("", "❗ Bu nomdagi kitob allaqachon mavjud.");
                return View(newBook);
            }

            _books.Add(newBook);
            TempData["Success"] = "✅ Yangi kitob muvaffaqiyatli qo‘shildi!";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/DeleteBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBook(string title)
        {
            var book = _books.FirstOrDefault(b => b.Title == title);
            if (book != null)
            {
                _books.Remove(book);
                TempData["Success"] = "🗑️ Kitob muvaffaqiyatli o‘chirildi.";
            }
            else
            {
                TempData["Error"] = "❌ Kitob topilmadi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Statistics
        public IActionResult Statistics()
        {
            var model = new
            {
                TotalBooks = _books.Count,
                RomanCount = _books.Count(b => b.Category == "Romanlar"),
                IlmiyCount = _books.Count(b => b.Category == "Ilmiy adabiyotlar")
            };

            return View(model);
        }
    }
}