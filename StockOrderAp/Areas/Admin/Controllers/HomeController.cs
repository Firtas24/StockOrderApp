using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;

namespace StockOrderApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var categoryCount = await _db.Categories.CountAsync();
            var productCount = await _db.Products.CountAsync();
            var outOfStockCount = await _db.Products.CountAsync(p => p.Stock == 0);
            var lowStockCount = await _db.Products.CountAsync(p => p.Stock > 0 && p.Stock <= 5);

            ViewBag.CategoryCount = categoryCount;
            ViewBag.ProductCount = productCount;
            ViewBag.OutOfStockCount = outOfStockCount;
            ViewBag.LowStockCount = lowStockCount;

            return View();
        }
    }
}
