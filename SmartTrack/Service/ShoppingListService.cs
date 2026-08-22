using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;

namespace SmartTrack.Services
{
    public class ShoppingListService
    {
        private readonly ApplicationDbContext _context;

        public ShoppingListService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateShoppingListAsync(
            string userId,
            IEnumerable<PurchaseRecommendationViewModel> recommendations)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            if (recommendations == null)
                return;

            // ---------------------------------------------------------
            // Get current active shopping list
            // ---------------------------------------------------------

            var shoppingList = await _context.ShoppingLists
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.Status == "ACTIVE");

            // ---------------------------------------------------------
            // Create list if it doesn't exist
            // ---------------------------------------------------------

            if (shoppingList == null)
            {
                shoppingList = new ShoppingList
                {
                    UserId = userId,
                    CreatedDate = DateTime.Now,
                    Status = "ACTIVE"
                };

                _context.ShoppingLists.Add(shoppingList);

                await _context.SaveChangesAsync();
            }

            // ---------------------------------------------------------
            // Add recommendations
            // PURCHASE NOW = <= 0
            // DUE SOON     = 1 - 3
            //
            // UPCOMING / OK are not added automatically.
            // ---------------------------------------------------------

            foreach (var recommendation in recommendations)
            {
                if (recommendation == null)
                    continue;

                if (string.IsNullOrWhiteSpace(recommendation.Product))
                    continue;

                if (recommendation.DaysUntilPurchase > 3)
                    continue;

                // -----------------------------------------------------
                // Check whether product already exists in active list
                // -----------------------------------------------------

                var existingItem = shoppingList.Items
                    .FirstOrDefault(x =>
                        x.Product.ToLower() ==
                        recommendation.Product.ToLower() &&
                        !x.IsPurchased);

                if (existingItem != null)
                {
                    // Update recommendation information
                    existingItem.Quantity =
                        (decimal)recommendation.LatestQuantity;

                    existingItem.Priority =
                        recommendation.Priority;

                    existingItem.RecommendationStatus =
                        recommendation.Status;

                    existingItem.ExpectedPurchaseDate =
      string.IsNullOrWhiteSpace(recommendation.ExpectedPurchaseDate)
          ? (DateTime?)null
          : DateTime.Parse(recommendation.ExpectedPurchaseDate);
                    


                    existingItem.DaysUntilPurchase =
                        recommendation.DaysUntilPurchase;

                    continue;
                }

                // -----------------------------------------------------
                // Create new item
                // -----------------------------------------------------

                var shoppingItem = new ShoppingListItem
                {
                    ShoppingListId = shoppingList.Id,

                    Product = recommendation.Product,

                    Quantity = (decimal)recommendation.LatestQuantity,

                    Priority = recommendation.Priority,

                    RecommendationStatus =
                        recommendation.Status,
                    ExpectedPurchaseDate =
        string.IsNullOrWhiteSpace(recommendation.ExpectedPurchaseDate)
            ? (DateTime?)null
            : DateTime.Parse(recommendation.ExpectedPurchaseDate),


                    DaysUntilPurchase =
                        recommendation.DaysUntilPurchase,

                    IsPurchased = false,

                    PurchasedDate = null,

                    CreatedDate = DateTime.Now
                };

                _context.ShoppingListItems.Add(shoppingItem);
            }

            await _context.SaveChangesAsync();
        }
    }
}