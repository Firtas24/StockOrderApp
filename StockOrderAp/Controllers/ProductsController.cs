using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;

namespace StockOrderApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // /Products?categoryId=..&search=..&sort=..
        public async Task<IActionResult> Index(int? categoryId, string? search, string? sort)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);


            // kategori filtresi
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            // arama (ürün adı + kategori adı)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Category != null && p.Category.Name.Contains(search))
                );
            }

            // sıralama
            query = sort switch
            {
                "priceAsc" => query.OrderBy(p => p.Price),
                "priceDesc" => query.OrderByDescending(p => p.Price),
                "stockDesc" => query.OrderByDescending(p => p.Stock),
                _ => query.OrderByDescending(p => p.Id) // default: en yeni
            };

            ViewBag.Categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(await query.AsNoTracking().ToListAsync());
        }

        // /Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                         .Include(p => p.Category)
                         .FirstOrDefaultAsync(p => p.Id == id);
                
            var reviews = await _db.ProductReviews
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Benzer ürünler (aynı kategori, kendisi hariç, stok > 0)
            var related = await _db.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.CategoryId == product.CategoryId &&
                    p.Id != product.Id &&
                    p.Stock > 0)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToListAsync();

            ViewBag.RelatedProducts = related;


            ViewBag.Reviews = reviews;
            ViewBag.AvgRating = reviews.Any() ? reviews.Average(x => x.Rating) : 0;



            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}
