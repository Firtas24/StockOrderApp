using System.ComponentModel.DataAnnotations;

namespace StockOrderApp.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(500)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Nav
        public Product? Product { get; set; }
    }
}
