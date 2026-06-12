using System.ComponentModel.DataAnnotations;

namespace StockOrderApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Kategori adı 2-60 karakter olmalı.")]
        public string Name { get; set; } = string.Empty;
    }
}
