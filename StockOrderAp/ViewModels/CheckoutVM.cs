using System.ComponentModel.DataAnnotations;
using StockOrderApp.ViewModels;

namespace StockOrderApp.ViewModels
{
    public class CheckoutVM
    {
        [Required, StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(400)]
        public string Address { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Note { get; set; }

        // Sepet özeti için:
        public CartVM Cart { get; set; } = new CartVM();
    }
}
