using Microsoft.AspNetCore.Mvc;
using SinovApp.Models;
using SinovApp.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SinovApp.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Har bir bo'lim uchun sahifalar
        public async Task<IActionResult> Romans() => 
            View("Romans", await _context.Books.Where(b => b.Category == "Romanlar").ToListAsync());

        public async Task<IActionResult> Science() => 
            View("Science", await _context.Books.Where(b => b.Category == "Ilmiy adabiyotlar").ToListAsync());

        public async Task<IActionResult> Textbooks() => 
            View("Textbooks", await _context.Books.Where(b => b.Category == "Darsliklar").ToListAsync());

        // 📥 Fayl yuklash
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/books", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/pdf";
            return File(fileBytes, contentType, fileName);
        }
    }
}