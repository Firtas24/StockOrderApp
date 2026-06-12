using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Models;

namespace StockOrderApp.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public FavoritesController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Favorites
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var favorites = await _db.UserFavorites
                .Where(x => x.UserId == userId)
                .Include(x => x.Product)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(favorites);
        }

        // POST: /Favorites/Toggle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = _userManager.GetUserId(User)!;

            var existing = await _db.UserFavorites
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId);

            if (existing is null)
            {
                _db.UserFavorites.Add(new UserFavorite { UserId = userId, ProductId = productId });
                TempData["success"] = "Favorilere eklendi.";
            }
            else
            {
                _db.UserFavorites.Remove(existing);
                TempData["success"] = "Favorilerden çıkarıldı.";
            }

            await _db.SaveChangesAsync();

            // geldiği sayfaya dön
            return Redirect(Request.Headers.Referer.ToString());
        }
    }
}
