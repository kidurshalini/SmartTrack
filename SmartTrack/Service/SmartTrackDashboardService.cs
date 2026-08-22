
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using System.Globalization;

namespace SmartTrack.Services
{
    public class SmartTrackStockService
    {
        private readonly ApplicationDbContext _context;

        // =========================================================
        // CONSUMPTION FACTORS
        // =========================================================

        private const decimal NORMAL_FACTOR = 1.0m;
        private const decimal HIGH_FACTOR = 1.5m;
        private const decimal MEDIUM_FACTOR = 1.0m;
        private const decimal LOW_FACTOR = 0.5m;
        private const decimal UNUSED_FACTOR = 0.0m;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SmartTrackStockService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET STOCK
        // =========================================================

        public async Task<SmartTrackStockState?> GetStockAsync(
            Guid householdId,
            string productName)
        {
            productName = productName.Trim();

            return await _context.SmartTrackStockStates
                .FirstOrDefaultAsync(x =>
                    x.HouseholdId == householdId &&
                    x.ProductName.ToLower() ==
                    productName.ToLower());
        }

        // =========================================================
        // GET OR CREATE STOCK
        // =========================================================

        public async Task<SmartTrackStockState> GetOrCreateStockAsync(
            string userId,
            Guid householdId,
            string productName,
            List<SmartTrackPurchaseHistoryDto> history)
        {
            productName = productName.Trim();

            var stock = await GetStockAsync(
                householdId,
                productName);

            var latestPurchase =
                GetLatestPurchase(history);

            if (latestPurchase == null)
            {
                if (stock != null)
                {
                    return stock;
                }

                return new SmartTrackStockState
                {
                    HouseholdId = householdId,
                    ProductName = productName,
                    CurrentStock = 0,
                    NormalDailyConsumption = 0,
                    LastProcessedDate = DateTime.Today,
                    UpdatedAt = DateTime.Now,
                    LastAdjustmentType = "NORMAL"
                };
            }

            DateTime purchaseDate =
                ParseDate(latestPurchase.PurchaseDate)
                ?? DateTime.Today;

            decimal purchaseQuantity =
                Convert.ToDecimal(latestPurchase.Quantity);

            // =====================================================
            // CREATE FIRST STOCK STATE
            // =====================================================

            if (stock == null)
            {
                decimal normalUsage =
                    CalculateNormalDailyConsumption(history);

                stock = new SmartTrackStockState
                {
                    HouseholdId = householdId,

                    ProductName = productName,

                    CurrentStock = purchaseQuantity,

                    LastPurchaseQuantity =
                        purchaseQuantity,

                    LastPurchaseDate =
                        purchaseDate,

                    NormalDailyConsumption =
                        normalUsage,

                    LastProcessedDate =
                        purchaseDate,

                    LastAdjustmentType =
                        "NORMAL",

                    LastAdjustmentDate =
                        null,

                    UpdatedAt =
                        DateTime.Now
                };

                _context.SmartTrackStockStates.Add(stock);

                await _context.SaveChangesAsync();

                return stock;
            }

            // =====================================================
            // CHECK FOR NEW PURCHASE
            // =====================================================

            DateTime existingPurchaseDate =
                stock.LastPurchaseDate;

            if (purchaseDate > existingPurchaseDate)
            {
                stock.CurrentStock =
                    purchaseQuantity;

                stock.LastPurchaseQuantity =
                    purchaseQuantity;

                stock.LastPurchaseDate =
                    purchaseDate;

                stock.LastProcessedDate =
                    purchaseDate;

                stock.NormalDailyConsumption =
                    CalculateNormalDailyConsumption(history);

                stock.LastAdjustmentType =
                    "NORMAL";

                stock.LastAdjustmentDate =
                    null;

                stock.UpdatedAt =
                    DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return stock;
        }

        // =========================================================
        // PROCESS STOCK
        // =========================================================

        public async Task<SmartTrackStockState> ProcessStockAsync(
            string userId,
            Guid householdId,
            string productName,
            List<SmartTrackPurchaseHistoryDto> history)
        {
            var stock =
                await GetOrCreateStockAsync(
                    userId,
                    householdId,
                    productName,
                    history);

            if (stock.LastPurchaseDate == DateTime.MinValue)
            {
                return stock;
            }

            DateTime today =
                DateTime.Today;

            DateTime lastProcessedDay =
                stock.LastProcessedDate.Date;

            // =====================================================
            // NOTHING TO PROCESS
            // =====================================================

            if (lastProcessedDay >= today)
            {
                return stock;
            }

            // =====================================================
            // PROCESS EVERY MISSED DAY
            // =====================================================

            DateTime processDate =
                lastProcessedDay.AddDays(1);

            while (processDate <= today)
            {
                await ProcessSingleDayAsync(
                    stock,
                    processDate);

                processDate =
                    processDate.AddDays(1);
            }

            stock.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return stock;
        }

        // =========================================================
        // PROCESS ONE DAY
        // =========================================================

        private async Task ProcessSingleDayAsync(
            SmartTrackStockState stock,
            DateTime date)
        {
            decimal normalUsage =
                stock.NormalDailyConsumption;

            // =====================================================
            // FIND USER'S DAILY BEHAVIOUR
            // =====================================================

            var adjustment =
                await _context
                    .SmartTrackStockAdjustments
                    .FirstOrDefaultAsync(x =>
                        x.HouseholdId ==
                        stock.HouseholdId &&

                        x.ProductName.ToLower() ==
                        stock.ProductName.ToLower() &&

                        x.AdjustmentDate.Date ==
                        date.Date);

            string usageType;
            decimal factor;
            bool automatic;

            // =====================================================
            // NO USER ENTRY = NORMAL
            // =====================================================

            if (adjustment == null)
            {
                usageType = "NORMAL";
                factor = NORMAL_FACTOR;
                automatic = true;
            }
            else
            {
                usageType =
                    adjustment.AdjustmentType
                        .Trim()
                        .ToUpperInvariant();

                factor =
                    GetAdjustmentFactor(
                        usageType);

                automatic = false;
            }

            // =====================================================
            // CALCULATE ACTUAL USAGE
            // =====================================================

            decimal actualUsage =
                normalUsage * factor;

            decimal stockBefore =
                stock.CurrentStock;

            decimal stockAfter =
                Math.Max(
                    0,
                    stockBefore - actualUsage);

            // =====================================================
            // SAVE DAILY USAGE
            // =====================================================

            var existingDailyRecord =
                await _context
                    .SmartTrackDailyUsages
                    .FirstOrDefaultAsync(x =>
                        x.HouseholdId ==
                        stock.HouseholdId &&

                        x.ProductName.ToLower() ==
                        stock.ProductName.ToLower() &&

                        x.UsageDate.Date ==
                        date.Date);

            if (existingDailyRecord == null)
            {
                var dailyUsage =
                    new SmartTrackDailyUsage
                    {
                        HouseholdId =
                            stock.HouseholdId,

                        ProductName =
                            stock.ProductName,

                        UsageDate =
                            date.Date,

                        UsageType =
                            usageType,

                        AdjustmentFactor =
                            factor,

                        NormalUsage =
                            normalUsage,

                        ActualUsage =
                            actualUsage,

                        StockBefore =
                            stockBefore,

                        StockAfter =
                            stockAfter,

                        IsAutomatic =
                            automatic,

                        CreatedAt =
                            DateTime.Now
                    };

                _context
                    .SmartTrackDailyUsages
                    .Add(dailyUsage);
            }

            // =====================================================
            // UPDATE STOCK
            // =====================================================

            stock.CurrentStock =
                stockAfter;

            stock.LastProcessedDate =
                date;

            stock.LastAdjustmentType =
                usageType;

            stock.LastAdjustmentDate =
                adjustment?.AdjustmentDate;

            stock.UpdatedAt =
                DateTime.Now;
        }

        // =========================================================
        // SAVE DAILY USER ADJUSTMENT
        // =========================================================

        public async Task<bool> SetDailyAdjustmentAsync(
            string userId,
            Guid householdId,
            string productName,
            DateTime date,
            string adjustmentType)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return false;
            }

            adjustmentType =
                adjustmentType
                    .Trim()
                    .ToUpperInvariant();

            if (!IsValidAdjustment(adjustmentType))
            {
                return false;
            }

            // =====================================================
            // VERIFY USER HOUSEHOLD
            // =====================================================

            bool householdUser =
                await _context
                    .UserHouseHoldDetails
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.HouseHoldId == householdId);

            if (!householdUser)
            {
                return false;
            }

            // =====================================================
            // FIND EXISTING ADJUSTMENT
            // =====================================================

            var existing =
                await _context
                    .SmartTrackStockAdjustments
                    .FirstOrDefaultAsync(x =>
                        x.HouseholdId ==
                        householdId &&

                        x.ProductName.ToLower() ==
                        productName.Trim().ToLower() &&

                        x.AdjustmentDate.Date ==
                        date.Date);

            if (existing == null)
            {
                existing =
                    new SmartTrackStockAdjustment
                    {
                        HouseholdId =
                            householdId,

                        UserId =
                            userId,

                        ProductName =
                            productName.Trim(),

                        AdjustmentDate =
                            date.Date,

                        AdjustmentType =
                            adjustmentType,

                        CreatedAt =
                            DateTime.Now
                    };

                _context
                    .SmartTrackStockAdjustments
                    .Add(existing);
            }
            else
            {
                existing.UserId =
                    userId;

                existing.AdjustmentType =
                    adjustmentType;
            }

            await _context.SaveChangesAsync();

            // =====================================================
            // REBUILD STOCK
            // =====================================================

            var history =
                await GetProductHistoryAsync(
                    householdId,
                    productName);

            var stock =
                await GetStockAsync(
                    householdId,
                    productName);

            if (stock != null)
            {
                await RebuildStockAsync(
                    stock,
                    history);
            }

            return true;
        }

        // =========================================================
        // REBUILD STOCK
        // =========================================================

        private async Task RebuildStockAsync(
            SmartTrackStockState stock,
            List<SmartTrackPurchaseHistoryDto> history)
        {
            var latestPurchase =
                GetLatestPurchase(history);

            if (latestPurchase == null)
            {
                return;
            }

            DateTime purchaseDate =
                ParseDate(
                    latestPurchase.PurchaseDate)
                ?? DateTime.Today;

            // =====================================================
            // RESET TO PURCHASE
            // =====================================================

            stock.CurrentStock =
                Convert.ToDecimal(
                    latestPurchase.Quantity);

            stock.LastPurchaseDate =
                purchaseDate;

            stock.LastPurchaseQuantity =
                Convert.ToDecimal(
                    latestPurchase.Quantity);

            stock.NormalDailyConsumption =
                CalculateNormalDailyConsumption(
                    history);

            stock.LastProcessedDate =
                purchaseDate;

            stock.LastAdjustmentType =
                "NORMAL";

            stock.LastAdjustmentDate =
                null;

            stock.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            // =====================================================
            // REPROCESS EACH DAY
            // =====================================================

            DateTime processDate =
                purchaseDate.Date.AddDays(1);

            while (processDate <= DateTime.Today)
            {
                await ProcessSingleDayAsync(
                    stock,
                    processDate);

                processDate =
                    processDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
        }

        // =========================================================
        // NORMAL DAILY CONSUMPTION
        // =========================================================

        public decimal CalculateNormalDailyConsumption(
            List<SmartTrackPurchaseHistoryDto> history)
        {
            if (history == null ||
                history.Count < 2)
            {
                return 0;
            }

            var purchases =
                history
                    .Select(x => new
                    {
                        Date =
                            ParseDate(
                                x.PurchaseDate),

                        Quantity =
                            Convert.ToDecimal(
                                x.Quantity)
                    })
                    .Where(x =>
                        x.Date.HasValue &&
                        x.Quantity > 0)
                    .OrderBy(x => x.Date)
                    .ToList();

            if (purchases.Count < 2)
            {
                return 0;
            }

            var usageValues =
                new List<decimal>();

            for (int i = 1;
                 i < purchases.Count;
                 i++)
            {
                decimal days =
                    Convert.ToDecimal(
                        (
                            purchases[i].Date!.Value -
                            purchases[i - 1].Date!.Value
                        ).TotalDays);

                if (days <= 0)
                {
                    continue;
                }

                decimal previousQuantity =
                    purchases[i - 1].Quantity;

                if (previousQuantity <= 0)
                {
                    continue;
                }

                decimal usage =
                    previousQuantity / days;

                usageValues.Add(usage);
            }

            if (usageValues.Count == 0)
            {
                return 0;
            }

            return Math.Round(
                usageValues.Average(),
                6);
        }

        // =========================================================
        // GET CURRENT STOCK
        // =========================================================

        public async Task<decimal> GetCurrentStockAsync(
            string userId,
            Guid householdId,
            string productName,
            List<SmartTrackPurchaseHistoryDto> history)
        {
            var stock =
                await ProcessStockAsync(
                    userId,
                    householdId,
                    productName,
                    history);

            return stock.CurrentStock;
        }

        // =========================================================
        // GET STOCK STATE
        // =========================================================

        public async Task<SmartTrackStockState> GetStockStateAsync(
            string userId,
            Guid householdId,
            string productName,
            List<SmartTrackPurchaseHistoryDto> history)
        {
            return await ProcessStockAsync(
                userId,
                householdId,
                productName,
                history);
        }

        // =========================================================
        // ADJUSTMENT FACTOR
        // =========================================================

        private decimal GetAdjustmentFactor(
            string type)
        {
            return type switch
            {
                "HIGH" =>
                    HIGH_FACTOR,

                "MEDIUM" =>
                    MEDIUM_FACTOR,

                "LOW" =>
                    LOW_FACTOR,

                "UNUSED" =>
                    UNUSED_FACTOR,

                "NORMAL" =>
                    NORMAL_FACTOR,

                _ =>
                    NORMAL_FACTOR
            };
        }

        // =========================================================
        // VALID ADJUSTMENT
        // =========================================================

        private bool IsValidAdjustment(
            string type)
        {
            return type switch
            {
                "HIGH" => true,
                "MEDIUM" => true,
                "LOW" => true,
                "UNUSED" => true,
                "NORMAL" => true,
                _ => false
            };
        }

        // =========================================================
        // LATEST PURCHASE
        // =========================================================

        private SmartTrackPurchaseHistoryDto?
            GetLatestPurchase(
                List<SmartTrackPurchaseHistoryDto> history)
        {
            return history
                .Where(x =>
                    !string.IsNullOrWhiteSpace(
                        x.PurchaseDate))
                .OrderByDescending(x =>
                    ParseDate(
                        x.PurchaseDate)
                    ?? DateTime.MinValue)
                .FirstOrDefault();
        }

        // =========================================================
        // GET PRODUCT HISTORY
        // =========================================================

        private async Task<
            List<SmartTrackPurchaseHistoryDto>>
            GetProductHistoryAsync(
                Guid householdId,
                string productName)
        {
            var householdUsers =
                await _context
                    .UserHouseHoldDetails
                    .Where(x =>
                        x.HouseHoldId ==
                        householdId)
                    .Select(x =>
                        x.UserId)
                    .Distinct()
                    .ToListAsync();

            return await (
                from item
                    in _context.ReceiptItems

                join receipt
                    in _context.Receipts
                    on item.ReceiptId
                        equals receipt.ReceiptId

                where
                    householdUsers.Contains(
                        receipt.CreatedBy)

                    && item.ItemName != null

                    && item.ItemName.ToLower() ==
                       productName.Trim().ToLower()

                orderby
                    receipt.PurchaseDate

                select new SmartTrackPurchaseHistoryDto
                {
                    ProductName =
                        item.ItemName,

                    Quantity =
                        item.Quantity,

                    PurchaseDate =
                        receipt.PurchaseDate
                            .ToString(
                                "yyyy-MM-ddTHH:mm:ss"),

                    UnitPrice =
                        (double)item.UnitPrice,

                    TotalPrice =
                        (double)item.TotalPrice,

                    Category =
                        "Unknown",

                    UserId =
                        receipt.CreatedBy,

                    ReceiptId =
                        receipt.ReceiptId
                }
            ).ToListAsync();
        }

        // =========================================================
        // DATE PARSER
        // =========================================================

        private DateTime? ParseDate(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
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

            return null;
        }
    }
}

