//using Microsoft.EntityFrameworkCore;
//using SmartTrack.Common;
//using SmartTrack.Models;

//namespace SmartTrack.Services
//{
//    public class SmartTrackPurchaseHistoryService
//    {
//        private readonly ApplicationDbContext _context;

//        public SmartTrackPurchaseHistoryService(
//            ApplicationDbContext context)
//        {
//            _context = context;
//        }


//        // =========================================================
//        // GET ALL HOUSEHOLD PURCHASE HISTORY
//        // =========================================================

//        public async Task<List<SmartTrackPurchaseHistoryDto>>
//            GetHouseholdPurchaseHistoryAsync(
//                string userId,
//                Guid householdId)
//        {
//            // -----------------------------------------------------
//            // 1. Verify logged-in user belongs to household
//            // -----------------------------------------------------

//            var belongsToHousehold =
//                await _context.UserHouseHoldDetails
//                    .AnyAsync(x =>
//                        x.UserId == userId &&
//                        x.HouseHoldId == householdId);

//            if (!belongsToHousehold)
//            {
//                return new List<SmartTrackPurchaseHistoryDto>();
//            }


//            // -----------------------------------------------------
//            // 2. Get ALL users belonging to household
//            // -----------------------------------------------------

//            var householdUserIds =
//                await _context.UserHouseHoldDetails
//                    .Where(x =>
//                        x.HouseHoldId == householdId)
//                    .Select(x => x.UserId)
//                    .Distinct()
//                    .ToListAsync();


//            if (!householdUserIds.Any())
//            {
//                return new List<SmartTrackPurchaseHistoryDto>();
//            }


//            // -----------------------------------------------------
//            // 3. Get receipts + receipt items
//            // -----------------------------------------------------

//            var history =
//                await (
//                    from receiptItem in _context.ReceiptItems

//                    join receipt in _context.Receipts
//                        on receiptItem.ReceiptId
//                        equals receipt.ReceiptId

//                    where householdUserIds.Contains(
//                        receipt.CreatedBy)

//                    orderby receipt.PurchaseDate ascending

//                    select new SmartTrackPurchaseHistoryDto
//                    {
//                        ProductName =
//                            receiptItem.ItemName,

//                        Quantity =
//                            receiptItem.Quantity,

//                        PurchaseDate =
//                            receipt.PurchaseDate.ToString("yyyy-MM-ddTHH:mm:ss"), // Convert DateTime to string

//                        UnitPrice =
//                            (double)receiptItem.UnitPrice,

//                        TotalPrice =
//                            (double)receiptItem.TotalPrice,

//                        Category =
//                            "Unknown",

//                        UserId =
//                            receipt.CreatedBy,

//                        ReceiptId =
//                            receipt.ReceiptId
//                    }

//                ).ToListAsync();


//            return history;
//        }


//        // =========================================================
//        // GET HISTORY FOR ONE PRODUCT
//        // =========================================================

//        public async Task<List<SmartTrackPurchaseHistoryDto>>
//            GetProductPurchaseHistoryAsync(
//                string userId,
//                Guid householdId,
//                string productName)
//        {
//            var history =
//                await GetHouseholdPurchaseHistoryAsync(
//                    userId,
//                    householdId);


//            return history
//                .Where(x =>
//                    string.Equals(
//                        x.ProductName,
//                        productName,
//                        StringComparison.OrdinalIgnoreCase))
//                .OrderBy(x => x.PurchaseDate)
//                .ToList();
//        }
//    }
//}

using Microsoft.EntityFrameworkCore;
using SmartTrack.Common;
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
        // GET ALL HOUSEHOLD PURCHASE HISTORY
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetHouseholdPurchaseHistoryAsync(
                string userId,
                Guid householdId)
        {
            var belongsToHousehold =
                await _context.UserHouseHoldDetails
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.HouseHoldId == householdId);

            if (!belongsToHousehold)
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }

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
                                .ToString("yyyy-MM-ddTHH:mm:ss"),

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
                    !string.IsNullOrWhiteSpace(x.ProductName) &&
                    string.Equals(
                        x.ProductName.Trim(),
                        productName.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PurchaseDate)
                .ToList();
        }


        // =========================================================
        // GET PRODUCT HISTORY
        // =========================================================

        public async Task<List<SmartTrackPurchaseHistoryDto>>
            GetProductHistoryAsync(
                string productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return new List<SmartTrackPurchaseHistoryDto>();
            }

            var searchName =
                productName.Trim().ToLower();

            var history =
                await (
                    from receiptItem in _context.ReceiptItems

                    join receipt in _context.Receipts
                        on receiptItem.ReceiptId
                        equals receipt.ReceiptId

                    where receiptItem.ItemName != null
                          &&
                          receiptItem.ItemName
                              .ToLower()
                              == searchName

                    orderby receipt.PurchaseDate ascending

                    select new SmartTrackPurchaseHistoryDto
                    {
                        ProductName =
                            receiptItem.ItemName,

                        Quantity =
                            receiptItem.Quantity,

                        PurchaseDate =
                            receipt.PurchaseDate
                                .ToString("yyyy-MM-ddTHH:mm:ss"),

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
        // PREDICTION
        // =========================================================

        public async Task<SmartTrackPredictionResult>
            PredictAsync(
                string productName,
                string stockLevel,
                List<SmartTrackPurchaseHistoryDto> history)
        {
            // -----------------------------------------------------
            // Validate history
            // -----------------------------------------------------

            if (history == null ||
                history.Count == 0)
            {
                return new SmartTrackPredictionResult
                {
                    PredictedDaysUntilPurchase = 0,
                    StockStatus = "No Data",
                    Recommendation =
                        "No purchase history is available for this product."
                };
            }


            // -----------------------------------------------------
            // Sort history
            // -----------------------------------------------------

            var orderedHistory =
                history
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.PurchaseDate))
                    .OrderBy(x => x.PurchaseDate)
                    .ToList();


            if (orderedHistory.Count == 0)
            {
                return new SmartTrackPredictionResult
                {
                    PredictedDaysUntilPurchase = 0,
                    StockStatus = "No Data",
                    Recommendation =
                        "No valid purchase dates were found."
                };
            }


            // -----------------------------------------------------
            // Convert purchase dates
            // -----------------------------------------------------

            var purchases =
                orderedHistory
                    .Select(x => new
                    {
                        Date = ParsePurchaseDate(
                            x.PurchaseDate),

                        Quantity = x.Quantity
                    })
                    .Where(x => x.Date.HasValue)
                    .Select(x => new
                    {
                        Date = x.Date!.Value,
                        x.Quantity
                    })
                    .OrderBy(x => x.Date)
                    .ToList();


            if (purchases.Count == 0)
            {
                return new SmartTrackPredictionResult
                {
                    PredictedDaysUntilPurchase = 0,
                    StockStatus = "No Data",
                    Recommendation =
                        "Unable to read purchase dates."
                };
            }


            // -----------------------------------------------------
            // If only one purchase exists
            // -----------------------------------------------------

            if (purchases.Count == 1)
            {
                return new SmartTrackPredictionResult
                {
                    PredictedDaysUntilPurchase = 30,
                    StockStatus = "Limited Data",
                    Recommendation =
                        "Only one purchase record is available. " +
                        "More purchase history is needed for a more accurate prediction."
                };
            }


            // =====================================================
            // CALCULATE PURCHASE INTERVALS
            // =====================================================

            var intervals =
                new List<double>();

            for (int i = 1; i < purchases.Count; i++)
            {
                var days =
                    (purchases[i].Date -
                     purchases[i - 1].Date)
                    .TotalDays;

                if (days > 0)
                {
                    intervals.Add(days);
                }
            }


            // -----------------------------------------------------
            // Average purchase interval
            // -----------------------------------------------------

            double averageInterval =
                intervals.Count > 0
                    ? intervals.Average()
                    : 30;


            // =====================================================
            // QUANTITY INFORMATION
            // =====================================================

            double averageQuantity =
                purchases
                    .Average(x =>
                        Convert.ToDouble(x.Quantity));


            double latestQuantity =
                Convert.ToDouble(
                    purchases.Last().Quantity);


            // =====================================================
            // ESTIMATE DAILY USAGE
            // =====================================================
            //
            // Quantity bought / number of days until next purchase
            //
            // Example:
            //
            // Soap:
            //
            // 1 → 4 in 28 days
            // 4 → 1 in 36 days
            // 1 → 4 in 23 days
            // 4 → 4 in 61 days
            //
            // This gives us a usage estimate.
            // =====================================================

            var dailyUsageValues =
                new List<double>();

            for (int i = 1; i < purchases.Count; i++)
            {
                var days =
                    (purchases[i].Date -
                     purchases[i - 1].Date)
                    .TotalDays;

                var previousQuantity =
                    Convert.ToDouble(
                        purchases[i - 1].Quantity);

                if (days > 0 &&
                    previousQuantity > 0)
                {
                    dailyUsageValues.Add(
                        previousQuantity / days);
                }
            }


            double averageDailyUsage;

            if (dailyUsageValues.Count > 0)
            {
                averageDailyUsage =
                    dailyUsageValues.Average();
            }
            else
            {
                averageDailyUsage =
                    averageQuantity /
                    Math.Max(1, averageInterval);
            }


            // =====================================================
            // ESTIMATE CURRENT STOCK
            // =====================================================
            //
            // We don't have a physical stock table in the
            // information supplied, so we estimate remaining stock
            // from the latest purchase and time elapsed.
            // =====================================================

            var latestPurchase =
                purchases.Last();

            var daysSincePurchase =
                Math.Max(
                    0,
                    (DateTime.Now -
                     latestPurchase.Date)
                    .TotalDays);


            double estimatedConsumed =
                averageDailyUsage *
                daysSincePurchase;


            double estimatedCurrentStock =
                Math.Max(
                    0,
                    latestQuantity -
                    estimatedConsumed);


            // =====================================================
            // PREDICT DAYS UNTIL NEXT PURCHASE
            // =====================================================

            int predictedDays;

            if (averageDailyUsage > 0)
            {
                predictedDays =
                    (int)Math.Round(
                        estimatedCurrentStock /
                        averageDailyUsage);
            }
            else
            {
                predictedDays =
                    (int)Math.Round(
                        averageInterval);
            }


            predictedDays =
                Math.Max(
                    0,
                    Math.Min(
                        predictedDays,
                        365));


            // =====================================================
            // STOCK STATUS
            // =====================================================

            string stockStatus;

            string recommendation;


            if (predictedDays <= 0)
            {
                stockStatus =
                    "Purchase Now";

                recommendation =
                    $"Your {productName} is likely due for purchase now.";
            }
            else if (predictedDays <= 3)
            {
                stockStatus =
                    "Critical";

                recommendation =
                    $"Purchase {productName} within the next {predictedDays} day(s).";
            }
            else if (predictedDays <= 7)
            {
                stockStatus =
                    "Low Stock";

                recommendation =
                    $"{productName} may need to be purchased within {predictedDays} days.";
            }
            else if (predictedDays <= 14)
            {
                stockStatus =
                    "Moderate";

                recommendation =
                    $"Monitor your {productName}. " +
                    $"The next purchase is estimated in about {predictedDays} days.";
            }
            else
            {
                stockStatus =
                    "Stock OK";

                recommendation =
                    $"No immediate purchase required for {productName}.";
            }


            // =====================================================
            // RETURN RESULT
            // =====================================================

            return new SmartTrackPredictionResult
            {
                PredictedDaysUntilPurchase =
                    predictedDays,

                StockStatus =
                    stockStatus,

                Recommendation =
                    recommendation
            };
        }


        // =========================================================
        // DATE PARSER
        // =========================================================

        private DateTime? ParsePurchaseDate(
            string value)
        {
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

            return null;
        }
    }


    // =============================================================
    // PREDICTION RESULT
    // =============================================================

    public class SmartTrackPredictionResult
    {
        public int PredictedDaysUntilPurchase { get; set; }

        public string StockStatus { get; set; } = "";

        public string Recommendation { get; set; } = "";
    }
}