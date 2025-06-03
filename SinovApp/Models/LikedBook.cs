using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinovApp.Models
{
    public class LikedBook
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public int BookId { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = default!;

        [ForeignKey("BookId")]
        public Book Book { get; set; } = default!;
    }
} 