using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockOrderApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Ürün adı 2-100 karakter olmalı.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 999999, ErrorMessage = "Fiyat 0 veya daha büyük olmalı.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Range(0, 999999, ErrorMessage = "Stok 0 veya daha büyük olmalı.")]
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;


        // Her ürün bir kategoriye bağlı
        [Required]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
        public string? ImagePath { get; set; }

    }
}
