using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SinovApp.Models;

namespace SinovApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }  // 👈 Bu qatormas yo‘q bo‘lsa, qo‘sh!
        public DbSet<LikedBook> LikedBooks { get; set; }
    }
}