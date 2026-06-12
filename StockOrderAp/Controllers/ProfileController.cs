using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockOrderAp.Data;
using StockOrderApp.Models;

namespace StockOrderApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Profile
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId! };
                _db.UserProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            ViewBag.Email = user?.Email;
            return View(profile);
        }

        // POST: /Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserProfile model)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var profile = await _db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null) return NotFound();

            profile.FullName = model.FullName;
            profile.Phone = model.Phone;
            profile.Address = model.Address;

            await _db.SaveChangesAsync();

            // İstersen Identity PhoneNumber da güncelleyebilirsin (opsiyonel)
            if (user != null && !string.IsNullOrWhiteSpace(model.Phone))
            {
                user.PhoneNumber = model.Phone;
                await _userManager.UpdateAsync(user);
            }

            TempData["success"] = "Profil güncellendi.";
            ViewBag.Email = user?.Email;

            return View(profile);
        }
    }
}
