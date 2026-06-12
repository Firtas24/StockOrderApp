using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Models;

namespace StockOrderApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Categories
        public async Task<IActionResult> Index(string? q, int page = 1)
        {
            const int pageSize = 10;

            var query = _db.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.Name.Contains(q));
            }

            var totalCount = await query.CountAsync();

            var list = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.Q = q;

            return View(list);
        }

        // GET: /Admin/Categories/Create
        public IActionResult Create()
        {
            return View(new Category());
        }

        // POST: /Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            model.Name = (model.Name ?? "").Trim();

            if (!ModelState.IsValid) return View(model);

            var exists = await _db.Categories.AnyAsync(x => x.Name.ToLower() == model.Name.ToLower());
            if (exists)
            {
                ModelState.AddModelError(nameof(Category.Name), "Bu kategori adı zaten mevcut.");
                return View(model);
            }

            _db.Categories.Add(model);
            await _db.SaveChangesAsync();

            TempData["success"] = "Kategori başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Categories.FindAsync(id);
            if (item == null) return NotFound();

            return View(item);
        }

        // POST: /Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            model.Name = (model.Name ?? "").Trim();

            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var item = await _db.Categories.FindAsync(id);
            if (item == null) return NotFound();

            var exists = await _db.Categories.AnyAsync(x => x.Id != id && x.Name.ToLower() == model.Name.ToLower());
            if (exists)
            {
                ModelState.AddModelError(nameof(Category.Name), "Bu kategori adı zaten mevcut.");
                return View(model);
            }

            item.Name = model.Name;
            await _db.SaveChangesAsync();

            TempData["success"] = "Kategori başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null) return NotFound();
            return View(item);
        }

        // POST: /Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _db.Categories.FindAsync(id);
            if (item == null) return NotFound();

            try
            {
                _db.Categories.Remove(item);
                await _db.SaveChangesAsync();
                TempData["success"] = "Kategori silindi.";
            }
            catch (DbUpdateException)
            {
                TempData["error"] = "Bu kategori kullanımda olduğu için silinemez. Önce bağlı kayıtları kaldırın/taşıyın.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
