using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SinovApp.Data;
using SinovApp.Models;

namespace SinovApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // SQL Serverga ulanish satri
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Xizmatlar
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new Exception("Client ID yo'q");
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new Exception("Client Secret yo'q");
                    options.CallbackPath = "/signin-google";
                    options.SaveTokens = true;
                    options.Events = new OAuthEvents
                    {
                        OnRemoteFailure = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/Account/Login?error=Google+authentication+failed");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("User", policy => policy.RequireRole("User"));
            });

            var app = builder.Build();

            // Xatolik sahifasi
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Middlewarelar
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Database migration'larni avtomatik qo'llash
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            // Rollar va admin yaratish (vaqtinchalik kommentariyaga olindi)
            // await SeedRolesAndAdminAsync(app);

            await app.RunAsync();
        }

        private static async Task SeedRolesAndAdminAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            const string adminEmail = "oyatullohmuxtorov5@gmail.com";
            // const string adminPassword = "Oyatulloh5523.08@sharq"; // Hardcoded parol o'chirildi
            
            // Admin parolini Environment Variable'dan olish
            var adminPassword = builder.Configuration["AdminPassword"];
            if (string.IsNullOrEmpty(adminPassword))
            {
                // Agar environment variable topilmasa, xatolik chiqarish yoki default qiymat berish
                // Production uchun environment variable qo'yish talab qilinadi
                throw new Exception("Admin paroli Environment Variable sifatida sozlangan bo'lishi kerak (AdminPassword). Yoki developmentda appsettings.json dan oling.");
            }

            var user = await userManager.FindByEmailAsync(adminEmail);
            if (user == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "Oyatulloh",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Oyatulloh Muxtorov"
                };

                var result = await userManager.CreateAsync(newAdmin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    Console.WriteLine("✅ Admin foydalanuvchi yaratildi.");
                }
                else
                {
                    Console.WriteLine("❌ Yaratishda xatolik: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, "Admin"))
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine("🔁 Mavjud foydalanuvchiga Admin roli qo'shildi.");
                }
            }
        }
    }
}