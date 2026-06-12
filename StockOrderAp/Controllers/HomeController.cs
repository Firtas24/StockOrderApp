using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderAp.Models;
using StockOrderApp.Models;

namespace StockOrderAp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Öne çýkan / en yeni ürünler (Sadece aktif)
            var featured = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToListAsync();

            ViewBag.FeaturedProducts = featured;

            // Kategoriler (aktif ürünlerde kullanýlan)
            var categories = await _db.Products
                .Where(p => p.IsActive && p.Category != null)
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;

            // Sana Özel Öneriler
            var recommended = new List<Product>();
            var userId = _userManager.GetUserId(User);

            if (!string.IsNullOrEmpty(userId))
            {
                var userCategoryIds = await _db.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Order != null && oi.Order.UserId == userId)
                    .Select(oi => oi.Product!.CategoryId)
                    .Distinct()
                    .Take(3)
                    .ToListAsync();

                if (userCategoryIds.Any())
                {
                    recommended = await _db.Products
                        .Include(p => p.Category)
                        .Where(p => p.IsActive && userCategoryIds.Contains(p.CategoryId))
                        .OrderByDescending(p => p.Id)
                        .Take(6)
                        .ToListAsync();
                }
            }

            // Fallback
            if (!recommended.Any())
            {
                recommended = await _db.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Id)
                    .Take(6)
                    .ToListAsync();
            }

            ViewBag.Recommended = recommended;

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
