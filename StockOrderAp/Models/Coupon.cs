using System.ComponentModel.DataAnnotations;

namespace StockOrderApp.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required, MaxLength(32)]
        public string Code { get; set; } = "";

        public int? Percent { get; set; }
        public decimal? Amount { get; set; }

        public decimal MinCartTotal { get; set; } = 0;

        public int UsageLimit { get; set; } = 0;
        public int UsedCount { get; set; } = 0;

        public DateTime? ExpireAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
