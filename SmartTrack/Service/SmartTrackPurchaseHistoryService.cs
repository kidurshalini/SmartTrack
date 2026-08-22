using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using System.Globalization;

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
        // GET USER HOUSEHOLD
        // =========================================================

        public async Task<UserHouseHoldDetails?> GetUserHouseholdAsync(
            string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }


        // =========================================================
        // GET HOUSEHOLD ID
        // =========================================================

        public async Task<Guid?> GetHouseholdIdAsync(
            string userId)
        {
            var household =
                await GetUserHouseholdAsync(userId);

            if (household == null)
            {
                return null;
            }

            return household.HouseHoldId;
        }


        // =========================================================
        // GET ALL USERS IN HOUSEHOLD
        // =========================================================

        public async Task<List<string>> GetHouseholdUserIdsAsync(
            Guid householdId)
        {
            if (householdId == Guid.Empty)
            {
                return new List<string>();
            }

            return await _context.UserHouseHoldDetails
                .Where(x => x.HouseHoldId == householdId)
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();
        }


        // =========================================================
        // GET ALL HOUSEHOLD PURCHASE HISTORY
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetHouseholdPurchaseHistoryAsync(
                string userId,
                Guid householdId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }

            if (householdId == Guid.Empty)
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }


            // -----------------------------------------------------
            // VERIFY USER BELONGS TO HOUSEHOLD
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
            // GET ALL USERS IN HOUSEHOLD
            // -----------------------------------------------------

            var householdUserIds =
                await GetHouseholdUserIdsAsync(
                    householdId);

            if (householdUserIds.Count == 0)
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }


            // -----------------------------------------------------
            // GET RECEIPTS + RECEIPT ITEMS
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
                            receipt.PurchaseDate
                                .ToString(
                                    "yyyy-MM-ddTHH:mm:ss"),

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
        // GET PRODUCT HISTORY FOR HOUSEHOLD
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetProductPurchaseHistoryAsync(
                string userId,
                Guid householdId,
                string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }


            var history =
                await GetHouseholdPurchaseHistoryAsync(
                    userId,
                    householdId);


            string searchName =
                productName.Trim();


            return history
                .Where(x =>
                    !string.IsNullOrWhiteSpace(
                        x.ProductName) &&
                    string.Equals(
                        x.ProductName.Trim(),
                        searchName,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x =>
                    ParsePurchaseDate(
                        x.PurchaseDate))
                .ToList();
        }


        // =========================================================
        // GET PRODUCT HISTORY USING USER'S HOUSEHOLD
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetProductPurchaseHistoryAsync(
                string userId,
                string productName)
        {
            var householdId =
                await GetHouseholdIdAsync(userId);

            if (!householdId.HasValue ||
                householdId.Value == Guid.Empty)
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }

            return await GetProductPurchaseHistoryAsync(
                userId,
                householdId.Value,
                productName);
        }


        // =========================================================
        // GET PRODUCT HISTORY - ALL USERS
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetProductHistoryAsync(
                string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }

            string searchName =
                productName.Trim();


            var history =
                await (
                    from receiptItem in _context.ReceiptItems

                    join receipt in _context.Receipts
                        on receiptItem.ReceiptId
                        equals receipt.ReceiptId

                    where receiptItem.ItemName != null

                    orderby receipt.PurchaseDate ascending

                    select new SmartTrackPurchaseHistoryDto
                    {
                        ProductName =
                            receiptItem.ItemName,

                        Quantity =
                            receiptItem.Quantity,

                        PurchaseDate =
                            receipt.PurchaseDate
                                .ToString(
                                    "yyyy-MM-ddTHH:mm:ss"),

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


            return history
                .Where(x =>
                    !string.IsNullOrWhiteSpace(
                        x.ProductName) &&
                    string.Equals(
                        x.ProductName.Trim(),
                        searchName,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x =>
                    ParsePurchaseDate(
                        x.PurchaseDate))
                .ToList();
        }


        // =========================================================
        // DEBUG HOUSEHOLD INFORMATION
        // =========================================================

        public async Task<SmartTrackHouseholdDebugInfo>
            GetHouseholdDebugInfoAsync(
                string userId,
                string productName)
        {
            var result =
                new SmartTrackHouseholdDebugInfo
                {
                    UserId = userId,
                    ProductName = productName
                };


            // -----------------------------------------------------
            // GET HOUSEHOLD
            // -----------------------------------------------------

            var household =
                await GetUserHouseholdAsync(userId);

            if (household == null)
            {
                return result;
            }


            result.HouseholdId =
                household.HouseHoldId;


            // -----------------------------------------------------
            // GET HOUSEHOLD USERS
            // -----------------------------------------------------

            result.HouseholdUserIds =
                await GetHouseholdUserIdsAsync(
                    household.HouseHoldId);


            // -----------------------------------------------------
            // GET PRODUCT HISTORY
            // -----------------------------------------------------

            result.PurchaseHistory =
                await GetProductPurchaseHistoryAsync(
                    userId,
                    household.HouseHoldId,
                    productName);


            return result;
        }


        // =========================================================
        // DATE PARSER
        // =========================================================

        private DateTime ParsePurchaseDate(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }


            if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
            {
                return result;
            }


            if (DateTime.TryParse(
                value,
                out result))
            {
                return result;
            }


            return DateTime.MinValue;
        }
    }


    // =============================================================
    // DEBUG RESULT
    // =============================================================

    public class SmartTrackHouseholdDebugInfo
    {
        public string UserId { get; set; } = "";

        public Guid? HouseholdId { get; set; }

        public string ProductName { get; set; } = "";

        public List<string> HouseholdUserIds { get; set; }
            = new();

        public List<SmartTrackPurchaseHistoryDto>
            PurchaseHistory
        { get; set; }
            = new();
    }
}