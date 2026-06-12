using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Helpers;
using StockOrderApp.Models;
using StockOrderApp.ViewModels;

namespace StockOrderApp.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        private const string CartKey = "cart_v1";
        private const string CouponKey = "coupon_code_v1"; // ✅ CartController ile birebir aynı olmalı

        public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cartVm = await BuildCartVMAsync();
            if (!cartVm.Lines.Any())
                return RedirectToAction("Index", "Cart");

            var (couponCode, discount, grandTotal, coupon) = await CalculateCouponTotalsAsync(cartVm.Subtotal);

            // Checkout ekranında göstermek için
            ViewBag.Subtotal = cartVm.Subtotal;
            ViewBag.Discount = discount;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.CouponCode = couponCode;

            var vm = new CheckoutVM { Cart = cartVm };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutVM vm)
        {
            var cartVm = await BuildCartVMAsync();
            vm.Cart = cartVm;

            if (!cartVm.Lines.Any())
            {
                ModelState.AddModelError("", "Sepet boş.");
                return await ReturnCheckoutWithTotals(vm);
            }

            if (!ModelState.IsValid)
                return await ReturnCheckoutWithTotals(vm);

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // ✅ Stok kontrolü (DB'den)
                foreach (var line in cartVm.Lines)
                {
                    var p = await _db.Products.FindAsync(line.ProductId);
                    if (p == null)
                    {
                        ModelState.AddModelError("", $"Ürün bulunamadı: {line.Name}");
                        await tx.RollbackAsync();
                        return await ReturnCheckoutWithTotals(vm);
                    }

                    if (p.Stock < line.Quantity)
                    {
                        ModelState.AddModelError("", $"{p.Name} için stok yetersiz. Mevcut stok: {p.Stock}");
                        await tx.RollbackAsync();
                        return await ReturnCheckoutWithTotals(vm);
                    }
                }

                // ✅ Kupon + indirim
                var subtotal = cartVm.Subtotal;
                var (couponCode, discount, grandTotal, coupon) = await CalculateCouponTotalsAsync(subtotal);

                var order = new Order
                {
                    UserId = userId,
                    FullName = vm.FullName,
                    Phone = vm.Phone,
                    Address = vm.Address,
                    Note = vm.Note,
                    CreatedAt = DateTime.UtcNow,

                    Total = subtotal,
                    CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode,
                    DiscountTotal = discount,
                    GrandTotal = grandTotal
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(); // Order.Id

                foreach (var line in cartVm.Lines)
                {
                    var p = await _db.Products.FindAsync(line.ProductId);
                    if (p == null) continue;

                    p.Stock -= line.Quantity;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = p.Id,
                        Quantity = line.Quantity,
                        UnitPrice = p.Price,
                        LineTotal = p.Price * line.Quantity
                    });
                }

                // ✅ Kupon kullanım sayısı
                if (coupon != null && !string.IsNullOrWhiteSpace(couponCode))
                    coupon.UsedCount += 1;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // ✅ Sepet + kupon temizle
                HttpContext.Session.Remove(CartKey);
                HttpContext.Session.Remove(CouponKey);

                return RedirectToAction("Success", new { id = order.Id });
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError("", "Sipariş oluşturulurken hata oluştu.");
                return await ReturnCheckoutWithTotals(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        // =========================
        // Helpers
        // =========================

        private async Task<CartVM> BuildCartVMAsync()
        {
            var cart = HttpContext.Session.GetObject<Dictionary<int, int>>(CartKey)
                       ?? new Dictionary<int, int>();

            var ids = cart.Keys.ToList();
            var products = ids.Count == 0
                ? new List<Product>()
                : await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

            var lines = products.Select(p => new CartLineVM
            {
                ProductId = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                Quantity = cart[p.Id]
            }).ToList();

            return new CartVM { Lines = lines };
        }

        private async Task<(string couponCode, decimal discount, decimal grandTotal, Coupon? coupon)>
            CalculateCouponTotalsAsync(decimal subtotal)
        {
            var couponCode = (HttpContext.Session.GetString(CouponKey) ?? "").Trim();
            decimal discount = 0m;
            Coupon? coupon = null;

            if (string.IsNullOrWhiteSpace(couponCode) || subtotal <= 0)
                return ("", 0m, subtotal, null);

            coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode);

            var invalid =
                coupon == null ||
                !coupon.IsActive ||
                (coupon.ExpireAt.HasValue && coupon.ExpireAt.Value < DateTime.Now) ||
                (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit) ||
                (subtotal < coupon.MinCartTotal);

            if (invalid)
            {
                HttpContext.Session.Remove(CouponKey);
                return ("", 0m, subtotal, null);
            }

            if (coupon!.Percent.HasValue && coupon.Percent.Value > 0)
                discount = subtotal * coupon.Percent.Value / 100m;
            else if (coupon.Amount.HasValue && coupon.Amount.Value > 0)
                discount = coupon.Amount.Value;

            if (discount > subtotal) discount = subtotal;

            var grandTotal = subtotal - discount;
            return (couponCode, discount, grandTotal, coupon);
        }

        private async Task<IActionResult> ReturnCheckoutWithTotals(CheckoutVM vm)
        {
            var subtotal = vm.Cart?.Subtotal ?? 0m;
            var (couponCode, discount, grandTotal, _) = await CalculateCouponTotalsAsync(subtotal);

            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.GrandTotal = grandTotal;
            ViewBag.CouponCode = couponCode;

            return View("Index", vm);
        }
    }
}
