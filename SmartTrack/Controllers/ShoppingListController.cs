using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModels;

namespace SmartTrack.Controllers
{
    [Authorize]
    public class ShoppingListController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser>
            _userManager;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public ShoppingListController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }


        // =====================================================
        // INDEX
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }


            // =================================================
            // GET ACTIVE SHOPPING LIST
            // =================================================

            var list =
                await _context.ShoppingLists
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.Status == "ACTIVE");


            // =================================================
            // NO ACTIVE LIST
            // =================================================

            if (list == null)
            {
                return View(
                    new ShoppingListViewModel
                    {
                        Status = "EMPTY",

                        TotalItems = 0,

                        PurchasedItems = 0,

                        RemainingItems = 0,

                        Items =
                            new List<ShoppingListItemViewModel>()
                    });
            }


            // =================================================
            // BUILD VIEW MODEL
            // =================================================

            var model =
                new ShoppingListViewModel
                {
                    Id = list.Id,

                    Status = list.Status,

                    CreatedDate = list.CreatedDate,

                    TotalItems =
                        list.Items.Count,

                    PurchasedItems =
                        list.Items.Count(
                            x => x.IsPurchased),

                    RemainingItems =
                        list.Items.Count(
                            x => !x.IsPurchased),

                    Items =
                        list.Items
                            .OrderBy(x => x.IsPurchased)
                            .ThenBy(x =>
                                x.DaysUntilPurchase)
                            .Select(x =>
                                new ShoppingListItemViewModel
                                {
                                    Id = x.Id,

                                    Product =
                                        x.Product,

                                    Quantity =
                                        x.Quantity,

                                    Priority =
                                        x.Priority,

                                    RecommendationStatus =
                                        x.RecommendationStatus,

                                    ExpectedPurchaseDate =
                                        x.ExpectedPurchaseDate,

                                    DaysUntilPurchase =
                                        x.DaysUntilPurchase,

                                    IsPurchased =
                                        x.IsPurchased,

                                    PurchasedDate =
                                        x.PurchasedDate
                                })
                            .ToList()
                };


            return View(model);
        }


        // =====================================================
        // MARK PURCHASED
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPurchased(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }


            var item =
                await _context.ShoppingListItems
                    .Include(x => x.ShoppingList)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.ShoppingList.UserId == userId);


            if (item == null)
            {
                return NotFound();
            }


            // =================================================
            // MARK PURCHASED
            // =================================================

            item.IsPurchased = true;

            item.PurchasedDate =
                DateTime.Now;


            await _context.SaveChangesAsync();


            // =================================================
            // CHECK REMAINING ITEMS
            // =================================================

            var remainingItems =
                await _context.ShoppingListItems
                    .CountAsync(x =>
                        x.ShoppingListId ==
                        item.ShoppingListId &&
                        !x.IsPurchased);


            // =================================================
            // COMPLETE LIST
            // =================================================

            if (remainingItems == 0)
            {
                item.ShoppingList.Status =
                    "COMPLETED";

                item.ShoppingList.CompletedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // REMOVE ITEM
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }


            var item =
                await _context.ShoppingListItems
                    .Include(x => x.ShoppingList)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.ShoppingList.UserId == userId);


            if (item == null)
            {
                return NotFound();
            }


            _context.ShoppingListItems.Remove(item);

            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Index));
        }
    }
}