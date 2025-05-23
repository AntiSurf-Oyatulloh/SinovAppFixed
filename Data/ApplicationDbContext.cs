using Microsoft.AspNetCore.Identity;
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

        public DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Book entity
            builder.Entity<Book>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Author)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FilePath)
                    .HasMaxLength(500);

                entity.Property(e => e.PdfLink)
                    .HasMaxLength(500);

                entity.Property(e => e.RealFileName)
                    .HasMaxLength(255);
            });
        }
    }
}