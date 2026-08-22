using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using System.Globalization;

namespace SmartTrack.Services
{
    public class SmartTrackStockService
    {
        private readonly ApplicationDbContext _context;

        private const decimal NORMAL_FACTOR = 1.0m;
        private const decimal HIGH_FACTOR = 1.5m;
        private const decimal MEDIUM_FACTOR = 1.0m;
        private const decimal LOW_FACTOR = 0.5m;
        private const decimal UNUSED_FACTOR = 0.0m;

        public SmartTrackStockService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PROCESS STOCK
        // =========================================================

        public async Task<SmartTrackStockState>
            ProcessStockAsync(
                string userId,
                Guid householdId,
                string productName,
                List<SmartTrackPurchaseHistoryDto> history)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new ArgumentException(
                    "Product name is required.");
            }

            productName = productName.Trim();

            history ??=
                new List<SmartTrackPurchaseHistoryDto>();

            var latestPurchase =
                GetLatestPurchase(history);

            if (latestPurchase == null)
            {
                return new SmartTrackStockState
                {
                    UserId = userId,
                    HouseholdId = householdId,
                    ProductName = productName,
                    CurrentStock = 0,
                    LastPurchaseQuantity = 0,
                    NormalDailyConsumption = 0,
                    AdaptiveConsumption = 0,
                    LastAdjustmentType = "NORMAL",
                    LastProcessedDate = DateTime.Today,
                    UpdatedAt = DateTime.Now
                };
            }

            DateTime purchaseDate =
                ParseDate(
                    latestPurchase.PurchaseDate)
                ?? DateTime.Today;

            decimal purchaseQuantity =
                Convert.ToDecimal(
                    latestPurchase.Quantity);

            decimal normalConsumption =
                CalculateNormalDailyConsumption(
                    history);

            if (normalConsumption < 0)
            {
                normalConsumption = 0;
            }

            var stock =
                await GetStockAsync(
                    householdId,
                    productName);

            // =====================================================
            // NEW STOCK OR NEW PURCHASE
            // =====================================================

            bool newPurchase =
                stock == null ||
                purchaseDate >
                stock.LastPurchaseDate;

            if (stock == null)
            {
                stock =
                    new SmartTrackStockState
                    {
                        UserId = userId,
                        HouseholdId = householdId,
                        ProductName = productName,
                        CurrentStock = purchaseQuantity,
                        LastPurchaseQuantity =
                            purchaseQuantity,
                        LastPurchaseDate =
                            purchaseDate,
                        NormalDailyConsumption =
                            normalConsumption,
                        AdaptiveConsumption =
                            normalConsumption,
                        LastProcessedDate =
                            purchaseDate.Date,
                        LastAdjustmentType =
                            "NORMAL",
                        UpdatedAt =
                            DateTime.Now
                    };

                _context.SmartTrackStockStates.Add(stock);

                await _context.SaveChangesAsync();
            }
            else if (newPurchase)
            {
                stock.UserId = userId;

                stock.CurrentStock =
                    purchaseQuantity;

                stock.LastPurchaseQuantity =
                    purchaseQuantity;

                stock.LastPurchaseDate =
                    purchaseDate;

                stock.NormalDailyConsumption =
                    normalConsumption;

                stock.AdaptiveConsumption =
                    normalConsumption;

                stock.LastProcessedDate =
                    purchaseDate.Date;

                stock.LastAdjustmentType =
                    "NORMAL";

                stock.LastAdjustmentDate =
                    null;

                stock.UpdatedAt =
                    DateTime.Now;

                await RemoveFutureDailyRecordsAsync(
                    stock,
                    purchaseDate.Date);

                await _context.SaveChangesAsync();
            }
            else
            {
                stock.NormalDailyConsumption =
                    normalConsumption;
            }

            // =====================================================
            // PROCESS COMPLETED DAYS
            // =====================================================

            DateTime firstDay =
                stock.LastProcessedDate.Date
                    .AddDays(1);

            DateTime lastCompletedDay =
                DateTime.Today.AddDays(-1);

            if (firstDay <= lastCompletedDay)
            {
                DateTime processDate =
                    firstDay;

                while (processDate <= lastCompletedDay)
                {
                    await ProcessSingleDayAsync(
                        stock,
                        processDate);

                    processDate =
                        processDate.AddDays(1);
                }
            }

            // =====================================================
            // CURRENT DAY
            // =====================================================
            //
            // We do not permanently process today's full usage.
            //
            // The user must have 24 hours before a normal daily
            // reduction is committed.
            //
            // This prevents the background service from reducing
            // the same day repeatedly.
            //
            // =====================================================

            string todayAdjustment =
                await GetAdjustmentTypeAsync(
                    stock.HouseholdId,
                    stock.ProductName,
                    DateTime.Today);

            decimal todayFactor =
                GetAdjustmentFactor(
                    todayAdjustment);

            stock.AdaptiveConsumption =
                stock.NormalDailyConsumption *
                todayFactor;

            stock.LastAdjustmentType =
                todayAdjustment;

            stock.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return stock;
        }

        // =========================================================
        // PROCESS ONE COMPLETED DAY
        // =========================================================

        private async Task ProcessSingleDayAsync(
            SmartTrackStockState stock,
            DateTime date)
        {
            date = date.Date;

            var existingRecord =
                await _context.SmartTrackDailyUsages
                    .FirstOrDefaultAsync(x =>
                        x.HouseholdId ==
                            stock.HouseholdId &&

                        x.ProductName.ToLower() ==
                            stock.ProductName.ToLower() &&

                        x.UsageDate == date);

            if (existingRecord != null)
            {
                stock.LastProcessedDate =
                    date;

                return;
            }

            string usageType =
                await GetAdjustmentTypeAsync(
                    stock.HouseholdId,
                    stock.ProductName,
                    date);

            decimal factor =
                GetAdjustmentFactor(
                    usageType);

            decimal normalUsage =
                stock.NormalDailyConsumption;

            decimal actualUsage =
                normalUsage * factor;

            decimal stockBefore =
                stock.CurrentStock;

            decimal stockAfter =
                Math.Max(
                    0,
                    stockBefore - actualUsage);

            var dailyUsage =
                new SmartTrackDailyUsage
                {
                    HouseholdId =
                        stock.HouseholdId,

                    ProductName =
                        stock.ProductName,

                    UsageDate =
                        date,

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
                        usageType == "NORMAL" &&
                        !await HasUserAdjustmentAsync(
                            stock.HouseholdId,
                            stock.ProductName,
                            date),

                    CreatedAt =
                        DateTime.Now
                };

            _context.SmartTrackDailyUsages
                .Add(dailyUsage);

            stock.CurrentStock =
                stockAfter;

            stock.LastProcessedDate =
                date;

            stock.LastAdjustmentType =
                usageType;

            var adjustment =
                await GetAdjustmentAsync(
                    stock.HouseholdId,
                    stock.ProductName,
                    date);

            stock.LastAdjustmentDate =
                adjustment?.AdjustmentDate;

            stock.AdaptiveConsumption =
                normalUsage * factor;

            stock.UpdatedAt =
                DateTime.Now;
        }

        // =========================================================
        // GET STOCK
        // =========================================================

        public async Task<SmartTrackStockState?>
            GetStockAsync(
                Guid householdId,
                string productName)
        {
            productName =
                productName.Trim();

            return await _context
                .SmartTrackStockStates
                .FirstOrDefaultAsync(x =>
                    x.HouseholdId ==
                        householdId &&

                    x.ProductName.ToLower() ==
                        productName.ToLower());
        }

        // =========================================================
        // GET CURRENT STOCK
        // =========================================================

        public async Task<decimal>
            GetCurrentStockAsync(
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
        // SAVE USER DAILY ADJUSTMENT
        // =========================================================

        public async Task<bool>
            SetDailyAdjustmentAsync(
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

            productName =
                productName.Trim();

            adjustmentType =
                adjustmentType
                    .Trim()
                    .ToUpperInvariant();

            if (!IsValidAdjustment(
                adjustmentType))
            {
                return false;
            }

            bool belongsToHousehold =
                await _context
                    .UserHouseHoldDetails
                    .AnyAsync(x =>
                        x.UserId == userId &&
                        x.HouseHoldId ==
                            householdId);

            if (!belongsToHousehold)
            {
                return false;
            }

            date =
                date.Date;

            var existing =
                await GetAdjustmentAsync(
                    householdId,
                    productName,
                    date);

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
                            productName,

                        AdjustmentDate =
                            date,

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

            decimal quantity =
                Convert.ToDecimal(
                    latestPurchase.Quantity);

            decimal normalUsage =
                CalculateNormalDailyConsumption(
                    history);

            stock.CurrentStock =
                quantity;

            stock.LastPurchaseQuantity =
                quantity;

            stock.LastPurchaseDate =
                purchaseDate;

            stock.NormalDailyConsumption =
                normalUsage;

            stock.AdaptiveConsumption =
                normalUsage;

            stock.LastProcessedDate =
                purchaseDate.Date;

            stock.LastAdjustmentType =
                "NORMAL";

            stock.LastAdjustmentDate =
                null;

            await _context.SaveChangesAsync();

            await RemoveDailyRecordsAfterPurchaseAsync(
                stock,
                purchaseDate.Date);

            DateTime processDate =
                purchaseDate.Date.AddDays(1);

            DateTime lastCompletedDay =
                DateTime.Today.AddDays(-1);

            while (processDate <=
                   lastCompletedDay)
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
        }

        // =========================================================
        // NORMAL DAILY CONSUMPTION
        // =========================================================

        public decimal
            CalculateNormalDailyConsumption(
                List<SmartTrackPurchaseHistoryDto>
                    history)
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

            var values =
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

                if (usage > 0)
                {
                    values.Add(usage);
                }
            }

            if (values.Count == 0)
            {
                return 0;
            }

            return Math.Round(
                values.Average(),
                6);
        }

        // =========================================================
        // GET ADJUSTMENT
        // =========================================================

        private async Task<string>
            GetAdjustmentTypeAsync(
                Guid householdId,
                string productName,
                DateTime date)
        {
            var adjustment =
                await GetAdjustmentAsync(
                    householdId,
                    productName,
                    date);

            if (adjustment == null)
            {
                return "NORMAL";
            }

            string type =
                adjustment.AdjustmentType
                    .Trim()
                    .ToUpperInvariant();

            return IsValidAdjustment(type)
                ? type
                : "NORMAL";
        }

        private async Task<SmartTrackStockAdjustment?>
            GetAdjustmentAsync(
                Guid householdId,
                string productName,
                DateTime date)
        {
            productName =
                productName.Trim();

            return await _context
                .SmartTrackStockAdjustments
                .FirstOrDefaultAsync(x =>
                    x.HouseholdId ==
                        householdId &&

                    x.ProductName.ToLower() ==
                        productName.ToLower() &&

                    x.AdjustmentDate ==
                        date.Date);
        }

        private async Task<bool>
            HasUserAdjustmentAsync(
                Guid householdId,
                string productName,
                DateTime date)
        {
            return await _context
                .SmartTrackStockAdjustments
                .AnyAsync(x =>
                    x.HouseholdId ==
                        householdId &&

                    x.ProductName.ToLower() ==
                        productName.ToLower() &&

                    x.AdjustmentDate ==
                        date.Date);
        }

        // =========================================================
        // FACTOR
        // =========================================================

        private decimal
            GetAdjustmentFactor(
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

                _ =>
                    NORMAL_FACTOR
            };
        }

        // =========================================================
        // VALID ADJUSTMENT
        // =========================================================

        private bool
            IsValidAdjustment(
                string type)
        {
            return type switch
            {
                "NORMAL" => true,
                "HIGH" => true,
                "MEDIUM" => true,
                "LOW" => true,
                "UNUSED" => true,
                _ => false
            };
        }

        // =========================================================
        // LATEST PURCHASE
        // =========================================================

        private SmartTrackPurchaseHistoryDto?
            GetLatestPurchase(
                List<SmartTrackPurchaseHistoryDto>
                    history)
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
        // PRODUCT HISTORY
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

                where
                    item.ItemName != null

                where
                    item.ItemName.ToLower() ==
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
        // REMOVE DAILY RECORDS AFTER PURCHASE
        // =========================================================

        private async Task
            RemoveDailyRecordsAfterPurchaseAsync(
                SmartTrackStockState stock,
                DateTime purchaseDate)
        {
            var records =
                await _context
                    .SmartTrackDailyUsages
                    .Where(x =>
                        x.HouseholdId ==
                            stock.HouseholdId &&

                        x.ProductName.ToLower() ==
                            stock.ProductName.ToLower() &&

                        x.UsageDate >=
                            purchaseDate.Date)
                    .ToListAsync();

            if (records.Count > 0)
            {
                _context
                    .SmartTrackDailyUsages
                    .RemoveRange(records);

                await _context.SaveChangesAsync();
            }
        }

        private async Task
            RemoveFutureDailyRecordsAsync(
                SmartTrackStockState stock,
                DateTime purchaseDate)
        {
            await RemoveDailyRecordsAfterPurchaseAsync(
                stock,
                purchaseDate);
        }

        // =========================================================
        // DATE PARSER
        // =========================================================

        private static DateTime?
            ParseDate(
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