using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Helpers;
using StockOrderApp.ViewModels;

namespace StockOrderApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        private const string CartKey = "cart_v1";      // productId -> quantity
        private const string CouponKey = "coupon_code_v1";  // applied coupon code (string)

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        // /Cart
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            var ids = cart.Keys.ToList();

            var products = ids.Count == 0
                ? new List<StockOrderApp.Models.Product>()
                : await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            var lines = products.Select(p => new CartLineVM
            {
                ProductId = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                Quantity = cart[p.Id]
            }).ToList();

            var vm = new CartVM { Lines = lines };

            // ✅ Totaller
            var subtotal = vm.Lines.Sum(x => x.Price * x.Quantity);

            // ✅ Kupon uygula (varsa)
            var couponCode = (HttpContext.Session.GetString(CouponKey) ?? "").Trim();
            decimal discount = 0m;

            if (!string.IsNullOrWhiteSpace(couponCode) && subtotal > 0)
            {
                // Code unique ise (bence unique olmalı) FirstOrDefault yeter
                var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);

                var invalid =
                    coupon == null ||
                    !coupon.IsActive ||
                    (coupon.ExpireAt.HasValue && coupon.ExpireAt.Value < DateTime.Now) ||
                    (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit) ||
                    (subtotal < coupon.MinCartTotal);

                if (invalid)
                {
                    HttpContext.Session.Remove(CouponKey);
                    couponCode = "";
                }
                else
                {
                    if (coupon!.Percent.HasValue && coupon.Percent.Value > 0)
                        discount = subtotal * coupon.Percent.Value / 100m;
                    else if (coupon.Amount.HasValue && coupon.Amount.Value > 0)
                        discount = coupon.Amount.Value;

                    if (discount > subtotal) discount = subtotal;
                }
            }

            var grandTotal = subtotal - discount;

            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.CouponCode = couponCode;

            return View(vm);
        }

        // /Cart/Add/5 (adet +1)
        public IActionResult Add(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();

            if (product.Stock <= 0)
            {
                TempData["error"] = "Bu ürün stokta yok.";
                return RedirectToAction(nameof(Index));
            }

            var cart = GetCart();

            cart.TryGetValue(id, out var qty);
            qty++;

            if (qty > product.Stock) qty = product.Stock;

            cart[id] = qty;
            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        // /Cart/Decrease/5 (adet -1)
        public IActionResult Decrease(int id)
        {
            var cart = GetCart();

            if (!cart.ContainsKey(id))
                return RedirectToAction(nameof(Index));

            cart[id]--;

            if (cart[id] <= 0)
                cart.Remove(id);

            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        // /Cart/Remove/5 (satırı sil)
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.Remove(id);
            SaveCart(cart);

            // Sepet boşaldıysa kuponu da kaldır (opsiyonel ama mantıklı)
            if (cart.Count == 0)
                HttpContext.Session.Remove(CouponKey);

            return RedirectToAction(nameof(Index));
        }

        // ✅ /Cart/ApplyCoupon (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["couponError"] = "Kupon kodu gir.";
                return RedirectToAction(nameof(Index));
            }

            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
            if (coupon == null || !coupon.IsActive)
            {
                TempData["couponError"] = "Kupon bulunamadı veya pasif.";
                return RedirectToAction(nameof(Index));
            }

            if (coupon.ExpireAt.HasValue && coupon.ExpireAt.Value < DateTime.Now)
            {
                TempData["couponError"] = "Kuponun süresi dolmuş.";
                return RedirectToAction(nameof(Index));
            }

            if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit)
            {
                TempData["couponError"] = "Kupon kullanım limiti dolmuş.";
                return RedirectToAction(nameof(Index));
            }

            // Sepet toplamını hesapla
            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["couponError"] = "Sepet boşken kupon uygulanamaz.";
                return RedirectToAction(nameof(Index));
            }

            var ids = cart.Keys.ToList();
            var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
            var subtotal = products.Sum(p => p.Price * cart[p.Id]);

            if (subtotal < coupon.MinCartTotal)
            {
                TempData["couponError"] = $"Bu kupon için minimum sepet: {coupon.MinCartTotal:N2} ₺";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetString(CouponKey, coupon.Code);

            TempData["couponSuccess"] = $"Kupon uygulandı: {coupon.Code}";
            return RedirectToAction(nameof(Index));
        }

        // ✅ /Cart/ClearCoupon
        public IActionResult ClearCoupon()
        {
            HttpContext.Session.Remove(CouponKey);
            TempData["couponSuccess"] = "Kupon kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        private Dictionary<int, int> GetCart()
        {
            return HttpContext.Session.GetObject<Dictionary<int, int>>(CartKey)
                   ?? new Dictionary<int, int>();
        }

        private void SaveCart(Dictionary<int, int> cart)
        {
            HttpContext.Session.SetObject(CartKey, cart);
        }
    }
}
