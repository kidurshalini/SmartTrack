using Microsoft.EntityFrameworkCore;
using SmartTrack.Common;
using SmartTrack.Models;

namespace SmartTrack.Services
{
    public class SmartTrackPurchaseHistoryService
    {
        private readonly ApplicationDbContext _context;

        public SmartTrackPurchaseHistoryService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // GET ALL HOUSEHOLD PURCHASE HISTORY
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetHouseholdPurchaseHistoryAsync(
                string userId,
                Guid householdId)
        {
            // -----------------------------------------------------
            // 1. Verify logged-in user belongs to household
            // -----------------------------------------------------

            var belongsToHousehold =
                await _context.UserHouseHoldDetails
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.HouseHoldId == householdId);

            if (!belongsToHousehold)
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }


            // -----------------------------------------------------
            // 2. Get ALL users belonging to household
            // -----------------------------------------------------

            var householdUserIds =
                await _context.UserHouseHoldDetails
                    .Where(x =>
                        x.HouseHoldId == householdId)
                    .Select(x => x.UserId)
                    .Distinct()
                    .ToListAsync();


            if (!householdUserIds.Any())
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }


            // -----------------------------------------------------
            // 3. Get receipts + receipt items
            // -----------------------------------------------------

            var history =
                await (
                    from receiptItem in _context.ReceiptItems

                    join receipt in _context.Receipts
                        on receiptItem.ReceiptId
                        equals receipt.ReceiptId

                    where householdUserIds.Contains(
                        receipt.CreatedBy)

                    orderby receipt.PurchaseDate ascending

                    select new SmartTrackPurchaseHistoryDto
                    {
                        ProductName =
                            receiptItem.ItemName,

                        Quantity =
                            receiptItem.Quantity,

                        PurchaseDate =
                            receipt.PurchaseDate.ToString("yyyy-MM-ddTHH:mm:ss"), // Convert DateTime to string

                        UnitPrice =
                            (double)receiptItem.UnitPrice,

                        TotalPrice =
                            (double)receiptItem.TotalPrice,

                        Category =
                            "Unknown",

                        UserId =
                            receipt.CreatedBy,

                        ReceiptId =
                            receipt.ReceiptId
                    }

                ).ToListAsync();


            return history;
        }


        // =========================================================
        // GET HISTORY FOR ONE PRODUCT
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetProductPurchaseHistoryAsync(
                string userId,
                Guid householdId,
                string productName)
        {
            var history =
                await GetHouseholdPurchaseHistoryAsync(
                    userId,
                    householdId);


            return history
                .Where(x =>
                    string.Equals(
                        x.ProductName,
                        productName,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PurchaseDate)
                .ToList();
        }
    }
}