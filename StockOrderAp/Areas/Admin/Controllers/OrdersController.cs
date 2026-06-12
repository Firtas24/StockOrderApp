using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Helpers;

namespace StockOrderApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Orders?status=&q=&from=&to=
        public async Task<IActionResult> Index(string? status, string? q, DateTime? from, DateTime? to)
        {
            var query = _db.Orders.AsNoTracking().AsQueryable();

            // filtre: durum
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.Trim();
                query = query.Where(o => o.Status == status);
            }

            // filtre: arama (sipariş no, ad, telefon)
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o =>
                    o.Id.ToString().Contains(q) ||
                    (o.FullName != null && o.FullName.Contains(q)) ||
                    (o.Phone != null && o.Phone.Contains(q))
                );
            }

            // filtre: tarih aralığı (CreatedAt UTC ise ToUniversalTime ile de ayarlanabilir)
            if (from.HasValue)
            {
                var f = from.Value.Date;
                query = query.Where(o => o.CreatedAt >= f);
            }

            if (to.HasValue)
            {
                // gün sonu
                var t = to.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedAt < t);
            }

            var orders = await query
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            // View helpers
            ViewBag.StatusList = OrderStatus.All;
            ViewBag.SelectedStatus = status;
            ViewBag.Q = q;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // POST: /Admin/Orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            status = (status ?? "").Trim();

            if (!OrderStatus.All.Contains(status))
            {
                TempData["error"] = "Geçersiz durum seçildi.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // aynı durum seçildiyse gereksiz save olmasın
            if (order.Status != status)
            {
                order.Status = status;
                await _db.SaveChangesAsync();
            }

            TempData["success"] = $"Sipariş #{id} durumu güncellendi: {status}";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}
