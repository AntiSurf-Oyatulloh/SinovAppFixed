using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinovApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SinovApp.Models; // ApplicationUser uchun kerak
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SinovApp.Controllers
{
    [Authorize] // Faqat tizimga kirgan foydalanuvchilar kira oladi
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Foydalanuvchi menejerini qo'shdik
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, ILogger<ProfileController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // Foydalanuvchi profilini ko'rsatish actioni
        public async Task<IActionResult> Index()
        {
            // Tizimga kirgan foydalanuvchi ma'lumotlarini UserManager orqali olish
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Agar foydalanuvchi topilmasa
                 // User async topilmasa, autentifikatsiya muammosi bo'lishi mumkin
                return RedirectToAction("Login", "Account");
            }

            // Profil view'sina foydalanuvchi modelini uzatish
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Foydalanuvchi ID '{_userManager.GetUserId(User)}' yuklanmadi.");
            }

            // Tahrirlash formasi uchun user modelini uzatish
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model, IFormFile? uploadedImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Foydalanuvchi ID '{_userManager.GetUserId(User)}' yuklanmadi.");
            }

            // Model state validatsiyasidan Profile qismiga tegishli bo'lmagan xatolarni olib tashlash
            ModelState.Remove("UserName");
            ModelState.Remove("Email");
            ModelState.Remove("PasswordHash");
            // ... Identityga tegishli boshqa maydonlar ham

            if (!ModelState.IsValid)
            {
                // Model validatsiya xatolarini loglash (debug uchun foydali)
                // foreach (var modelStateEntry in ModelState.Where(e => e.Value.Errors.Any()))
                // {
                //     var propertyName = modelStateEntry.Key;
                //     var errors = modelStateEntry.Value.Errors;
                //     foreach (var error in errors)
                //     {
                //         _logger.LogError($"Profile Edit POST ModelState error: Property: {propertyName}, Error: {error.ErrorMessage}");
                //     }
                // }
                return View(model); // Xatolar bilan formani qaytarish
            }

            // Foydalanuvchi ma'lumotlarini yangilash
            user.FullName = model.FullName; // FullName yangilanadi
            user.Bio = model.Bio; // Bio yangilanadi

            // Profil rasmini yuklash va saqlash logikasi
            if (uploadedImage != null && uploadedImage.Length > 0)
            {
                 // Rasm turini tekshirish (AdminController.cs dagi IsValidFile ga o'xshash)
                var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
                if (!allowedImageTypes.Contains(uploadedImage.ContentType.ToLower()))
                {
                    ModelState.AddModelError("uploadedImage", "Faqat rasm formatidagi fayllar yuklanishi mumkin (JPG, PNG, GIF, BMP, WEBP)");
                    return View(model); // Xatolar bilan formani qaytarish
                }

                // Rasm hajmini tekshirish (5MB limit)
                if (uploadedImage.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("uploadedImage", "Rasm hajmi 5MB dan oshmasligi kerak");
                    return View(model); // Xatolar bilan formani qaytarish
                }

                // Fayl nomini xavfsizlashtirish va yo'lni yaratish
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(uploadedImage.FileName)}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "profile"); // Profil rasmlari uchun alohida papka

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    // Eski rasmini o'chirish (agar mavjud bo'lsa va default rasm bo'lmasa)
                    if (!string.IsNullOrEmpty(user.ImagePath) && user.ImagePath != "/images/default-profile.png")
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, user.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Yangi rasmini saqlash
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadedImage.CopyToAsync(stream);
                    }

                    user.ImagePath = "/images/profile/" + fileName; // Ma'lumotlar bazasida saqlanadigan yo'l
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profil rasmini yuklashda xatolik yuz berdi");
                    ModelState.AddModelError("uploadedImage", "Rasm yuklashda xatolik yuz berdi. Iltimos, qaytadan urinib ko'ring.");
                    return View(model); // Xatolar bilan formani qaytarish
                }
            }

            // Foydalanuvchi ma'lumotlarini bazada yangilash
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                 // Success xabarini ko'rsatish uchun TempData ishlatish
                 TempData["SuccessMessage"] = "✅ Profil muvaffaqiyatli yangilandi!";
                return RedirectToAction(nameof(Index)); // Profil sahifasiga qaytarish
            }
            else
            {
                // Xatolarni ModelState ga qo'shish
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model); // Xatolar bilan formani qaytarish
            }
        }

        // Qo'shimcha actionlar (masalan, Edit) keyinchalik qo'shilishi mumkin)

        // Mening kitoblarim actioni
        public async Task<IActionResult> MyBooks()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Foydalanuvchining "liked" kitoblarini yuklash
            // Ensure LikedBooks are included when fetching the user
            user = await _userManager.Users
                .Include(u => u.LikedBooks)
                    .ThenInclude(lb => lb.Book)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            // Explicitly load LikedBooks collection if it wasn't included (as a safeguard)
            if (user != null && !_context.Entry(user).Collection(u => u.LikedBooks).IsLoaded)
            {
                _context.Entry(user).Collection(u => u.LikedBooks).Load();
                // Ensure Book is loaded for each LikedBook
                foreach (var likedBook in user.LikedBooks)
                {
                    if (!_context.Entry(likedBook).Reference(lb => lb.Book).IsLoaded)
                    {
                        _context.Entry(likedBook).Reference(lb => lb.Book).Load();
                    }
                }
            }

            if (user == null) // Should not happen if GetUserAsync was successful, but as a safeguard
            {
                 return RedirectToAction("Login", "Account");
            }

            // Agar LikedBooks null bo'lsa, bo'sh ro'yxat yaratish
            var likedBooks = user.LikedBooks?.Select(lb => lb.Book).ToList() ?? new List<Book>();

            return View(likedBooks);
        }
    }
} 