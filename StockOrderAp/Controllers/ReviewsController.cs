using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Models;

namespace StockOrderApp.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int rating, string comment)
        {
            var userId = _userManager.GetUserId(User)!;

            // Aynı ürüne tekrar yorum atmasın
            var exists = await _db.ProductReviews
                .AnyAsync(x => x.ProductId == productId && x.UserId == userId);

            if (exists)
            {
                TempData["error"] = "Bu ürün için zaten yorum yaptın.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = (comment ?? "").Trim()
            };

            if (string.IsNullOrWhiteSpace(review.Comment))
            {
                TempData["error"] = "Yorum boş olamaz.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _db.ProductReviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["success"] = "Yorumun eklendi.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}
