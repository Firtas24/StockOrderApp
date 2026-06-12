using System.Linq;

namespace StockOrderApp.ViewModels
{
    public class CartVM
    {
        public List<CartLineVM> Lines { get; set; } = new();

        public decimal Subtotal => Lines.Sum(x => x.LineTotal);
        public int TotalItems => Lines.Sum(x => x.Quantity);
        public string? CouponCode { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal GrandTotal { get; set; }

    }

    public class CartLineVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int Quantity { get; set; }

        public decimal LineTotal => Price * Quantity;
    }
}
