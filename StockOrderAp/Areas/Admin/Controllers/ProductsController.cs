using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Models;

namespace StockOrderApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Dropdown için kategori listesini hazırlar
        private async Task LoadCategoriesAsync(int? selectedId = null)
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CategoryList = new SelectList(categories, "Id", "Name", selectedId);
        }

        // GET: /Admin/Products?search=...
        public async Task<IActionResult> Index(string? search)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Category != null && p.Category.Name.Contains(search))
                );
            }

            var products = await query
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.Search = search;
            return View(products);
        }

        // GET: /Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View(new Product());
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(product.CategoryId);
                return View(product);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var savedPath = await SaveProductImageAsync(imageFile);
                product.ImagePath = savedPath;
            }

            product.IsActive = true;

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            TempData["success"] = "Ürün eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            await LoadCategoriesAsync(product.CategoryId);
            return View(product);
        }

        // POST: /Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync(product.CategoryId);
                return View(product);
            }

            var dbProduct = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (dbProduct == null) return NotFound();

            dbProduct.Name = product.Name;
            dbProduct.Price = product.Price;
            dbProduct.Stock = product.Stock;
            dbProduct.CategoryId = product.CategoryId;

            // ✅ Durum (Aktif/Pasif) da güncellensin
            dbProduct.IsActive = product.IsActive;

            // ✅ Yeni görsel seçildiyse güncelle
            if (imageFile != null && imageFile.Length > 0)
            {
                var savedPath = await SaveProductImageAsync(imageFile);

                // Eski dosyayı sil (varsa)
                TryDeleteFile(dbProduct.ImagePath);

                // Yeni path'i DB'ye yaz
                dbProduct.ImagePath = savedPath;
            }

            await _db.SaveChangesAsync();

            TempData["success"] = "Ürün güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: /Admin/Products/Delete/5  (Soft delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (!product.IsActive)
            {
                TempData["success"] = "Ürün zaten pasif.";
                return RedirectToAction(nameof(Index));
            }

            product.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["success"] = "Ürün pasife alındı.";
            return RedirectToAction(nameof(Index));
        }

        // --- Helpers ---

        private async Task<string> SaveProductImageAsync(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Sadece jpg, jpeg, png, webp yüklenebilir.");

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // wwwroot sonrası URL path
            return $"/uploads/products/{fileName}";
        }

        private void TryDeleteFile(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            var relative = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relative);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
