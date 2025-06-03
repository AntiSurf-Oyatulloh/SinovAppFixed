using System.ComponentModel.DataAnnotations;

namespace SinovApp.Models
{
    public class SearchViewModel
    {
        [Display(Name = "Qidiruv so'zi")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Kategoriya")]
        public string? Category { get; set; }

        [Display(Name = "Saralash")]
        public string? SortBy { get; set; }

        public List<Book>? Books { get; set; }
    }
} 