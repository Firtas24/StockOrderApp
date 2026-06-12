using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using System.Text.Json;

namespace StockOrderApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // BUGÜN
            var todayOrders = await _db.Orders
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .ToListAsync();

            ViewBag.TodayOrderCount = todayOrders.Count;
            ViewBag.TodayRevenue = todayOrders.Sum(o => o.GrandTotal > 0 ? o.GrandTotal : o.Total);

            // TOPLAM
            ViewBag.TotalOrderCount = await _db.Orders.CountAsync();
            ViewBag.TotalRevenue = await _db.Orders.SumAsync(o => (decimal?)(o.GrandTotal > 0 ? o.GrandTotal : o.Total)) ?? 0;

            // KRİTİK STOK
            ViewBag.LowStock = await _db.Products
                .Where(p => p.Stock <= 5 && p.IsActive)
                .OrderBy(p => p.Stock)
                .Take(6)
                .ToListAsync();

            // EN ÇOK SATANLAR
            ViewBag.TopProducts = await _db.OrderItems
                .Include(i => i.Product)
                .GroupBy(i => new { i.ProductId, Name = i.Product!.Name })
                .Select(g => new
                {
                    Name = g.Key.Name,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

            // SON SİPARİŞLER
            ViewBag.LastOrders = await _db.Orders
                .OrderByDescending(o => o.Id)
                .Take(10)
                .ToListAsync();

            // ✅ GÜNLÜK SATIŞ GRAFİĞİ (Son 7 gün)
            var fromDate = DateTime.UtcNow.Date.AddDays(-6);
            var toDate = DateTime.UtcNow.Date.AddDays(1);

            var daily = await _db.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < toDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Total = g.Sum(x => (x.GrandTotal > 0 ? x.GrandTotal : x.Total))
                })
                .ToListAsync();

            var days = Enumerable.Range(0, 7).Select(i => fromDate.AddDays(i)).ToList();
            var map = daily.ToDictionary(x => x.Day, x => x.Total);

            var labels = days.Select(d => d.ToString("dd.MM")).ToList();
            var data = days.Select(d => map.ContainsKey(d) ? map[d] : 0m).ToList();

            ViewBag.ChartLabelsJson = JsonSerializer.Serialize(labels);
            ViewBag.ChartDataJson = JsonSerializer.Serialize(data);

            return View();
        }
    }
}
