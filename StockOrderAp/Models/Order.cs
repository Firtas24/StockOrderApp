using System.ComponentModel.DataAnnotations;

namespace StockOrderApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Identity user id (string) - login zorunlu yapacağız
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(400)]
        public string Address { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal Total { get; set; }

        public List<OrderItem> Items { get; set; } = new();
        public string Status { get; set; } = "Hazırlanıyor";
        public string? CouponCode { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal GrandTotal { get; set; }


    }
}

