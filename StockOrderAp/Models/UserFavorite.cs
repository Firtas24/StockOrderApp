using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockOrderApp.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }

        // Navigation (opsiyonel ama pro)
        public Product? Product { get; set; }
    }
}
