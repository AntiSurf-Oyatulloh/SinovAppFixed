using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SinovApp.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SinovApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (TempData["Error"] != null)
                ViewBag.Error = TempData["Error"];
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ✅ Email bo'yicha mavjud foydalanuvchini tekshiramiz
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                // 🔐 Parolni tekshiramiz
                var result = await _signInManager.CheckPasswordSignInAsync(existingUser, model.Password, false);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(existingUser, false);

                    // 🔄 Agar Admin bo'lsa → Admin panelga
                    if (await _userManager.IsInRoleAsync(existingUser, "Admin"))
                        return RedirectToAction("Index", "Admin");

                    // Aks holda → Bosh sahifa
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Bu email allaqachon ro'yxatdan o'tgan, lekin parol noto'g'ri.");
                    return View(model);
                }
            }

            // 🆕 Yangi foydalanuvchi yaratamiz
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = $"{model.FirstName} {model.LastName}",
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            // ❌ Agar foydalanuvchi yaratishda xatolik bo'lsa
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties 
            { 
                RedirectUri = redirectUrl,
                Items =
                {
                    { "scheme", GoogleDefaults.AuthenticationScheme }
                }
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            Console.WriteLine("➡️ GoogleResponse chaqirildi");

            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    Console.WriteLine("❌ info NULL. GoogleLogin info kelmadi.");
                    TempData["Error"] = "Google ma'lumotlarini olishda xatolik yuz berdi.";
                    return RedirectToAction("Login");
                }

                Console.WriteLine("✅ info keldi");
                Console.WriteLine($"👤 Email: {info.Principal.FindFirstValue(ClaimTypes.Email)}");
                Console.WriteLine($"🔑 Login Provider: {info.LoginProvider}");

                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("⚠️ Email yo'q");
                    TempData["Error"] = "Email ma'lumoti topilmadi.";
                    return RedirectToAction("Login");
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    Console.WriteLine("👤 Foydalanuvchi bazada bor");
                    var logins = await _userManager.GetLoginsAsync(user);
                    if (!logins.Any(l => l.LoginProvider == info.LoginProvider))
                    {
                        Console.WriteLine("🔗 Login provider bog'lanmagan, endi bog'layapmiz");
                        await _userManager.AddLoginAsync(user, info);
                    }

                    await _signInManager.SignInAsync(user, false);
                    var role = await _userManager.IsInRoleAsync(user, "Admin") ? "Admin" : "User";
                    Console.WriteLine($"🔄 Roli: {role}");

                    return RedirectToAction("Index", role);
                }

                Console.WriteLine("🆕 Yangi user yaratilyapti");
                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Foydalanuvchi",
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (createResult.Succeeded)
                {
                    await _userManager.AddLoginAsync(newUser, info);

                    var role = email.ToLower() == "oyatullohmuxtorov5@gmail.com" ? "Admin" : "User";
                    await _userManager.AddToRoleAsync(newUser, role);

                    await _signInManager.SignInAsync(newUser, false);
                    Console.WriteLine($"✅ Foydalanuvchi yaratildi va {role} rol berildi.");

                    return RedirectToAction("Index", role);
                }

                Console.WriteLine("❌ Foydalanuvchi yaratishda xato:");
                foreach (var error in createResult.Errors)
                    Console.WriteLine($"⚠️ {error.Description}");

                TempData["Error"] = "Yangi foydalanuvchini yaratishda xatolik yuz berdi.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Xatolik: {ex.Message}");
                TempData["Error"] = "Google autentifikatsiyasi vaqtida xatolik yuz berdi.";
                return RedirectToAction("Login");
            }
        }

        [Route("Account/AccessDenied")]
        public IActionResult AccessDenied() => View();

        [Authorize]
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties
            {
                RedirectUri = Url.Action("Index", "Home")
            }, CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}