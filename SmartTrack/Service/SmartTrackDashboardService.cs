using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using System.Globalization;

namespace SmartTrack.Services
{
    public class SmartTrackDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly SmartTrackPurchaseHistoryService _purchaseHistoryService;
        private readonly SmartTrackAIService _aiService;
        private readonly SmartTrackNotificationService _notificationService;
        private readonly SmartTrackStockService _stockService;

        public SmartTrackDashboardService(
            ApplicationDbContext context,
            SmartTrackPurchaseHistoryService purchaseHistoryService,
            SmartTrackAIService aiService,
            SmartTrackNotificationService notificationService,
            SmartTrackStockService stockService)
        {
            _context = context;
            _purchaseHistoryService = purchaseHistoryService;
            _aiService = aiService;
            _notificationService = notificationService;
            _stockService = stockService;
        }

        public async Task<SmartTrackDashboardViewModel> GetDashboardAsync(
            string userId)
        {
            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userHousehold == null)
            {
                throw new Exception(
                    "User is not connected to a household.");
            }

            Guid householdId = userHousehold.HouseHoldId;

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == userId);

            var model = new SmartTrackDashboardViewModel
            {
                UserName = user?.UserName ?? "User"
            };

            var history =
                await _purchaseHistoryService
                    .GetHouseholdPurchaseHistoryAsync(
                        userId,
                        householdId);

            var products =
                history
                    .Select(x => x.ProductName)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            foreach (var product in products)
            {
                var productHistory =
                    history
                        .Where(x =>
                            string.Equals(
                                x.ProductName,
                                product,
                                StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x => ParseDate(x.PurchaseDate))
                        .ToList();

                if (!productHistory.Any())
                {
                    continue;
                }

                SmartTrackPredictionResponse? result;

                try
                {
                    result =
                        await _aiService.PredictAsync(
                            product,
                            "MEDIUM",
                            productHistory);
                }
                catch
                {
                    continue;
                }

                if (result == null)
                {
                    continue;
                }

                string productName =
                    string.IsNullOrWhiteSpace(result.Product)
                        ? product
                        : result.Product;

                double latestQuantity =
                    result.LatestQuantity ?? 0;

                int daysUntilPurchase =
                    result.DaysUntilPurchase ?? 0;

                // =====================================================
                // REAL STOCK CALCULATION
                // =====================================================

                var stockState =
                    await _stockService.ProcessStockAsync(
                        userId,
                        householdId,
                        productName,
                        productHistory);

                double currentStock =
                    Convert.ToDouble(
                        stockState.CurrentStock);

                double normalConsumption =
                    Convert.ToDouble(
                        stockState.NormalDailyConsumption);

                double adaptiveConsumption =
                    Convert.ToDouble(
                        stockState.AdaptiveConsumption);

                // =====================================================
                // PURCHASE RECOMMENDATION
                // =====================================================

                var recommendation =
                    new PurchaseRecommendationViewModel
                    {
                        Product = productName,

                        LatestQuantity = latestQuantity,

                        LastPurchaseDate =
                            FormatDate(
                                result.LastPurchaseDate),

                        ExpectedPurchaseDate =
                            FormatDate(
                                result.ExpectedPurchaseDate),

                        DaysUntilPurchase =
                            daysUntilPurchase,

                        Status =
                            string.IsNullOrWhiteSpace(result.Status)
                                ? "NORMAL"
                                : result.Status,

                        Recommendation =
                            result.Recommendation
                            ?? string.Empty,

                        Anomaly =
                            result.Anomaly,

                        AnomalyStatus =
                            result.AnomalyStatus ?? "NORMAL",

                        AnomalyScore =
                            result.AnomalyScore ?? 0,

                        Priority =
                            GetPriority(daysUntilPurchase)
                    };

                model.PurchaseRecommendations
                    .Add(recommendation);

                // =====================================================
                // REAL STOCK STATUS
                // =====================================================

                int stockDaysUntilPurchase;

                if (adaptiveConsumption > 0)
                {
                    stockDaysUntilPurchase =
                        (int)Math.Floor(
                            currentStock /
                            adaptiveConsumption);
                }
                else
                {
                    stockDaysUntilPurchase = 365;
                }

                stockDaysUntilPurchase =
                    Math.Max(
                        0,
                        Math.Min(
                            stockDaysUntilPurchase,
                            365));

                var stockViewModel =
                    new StockStatusViewModel
                    {
                        Product =
                            productName,

                        LatestQuantity =
                            latestQuantity,

                        CurrentStock =
                            currentStock,

                        NormalDailyConsumption =
                            normalConsumption,

                        AdaptiveConsumption =
                            adaptiveConsumption,

                        AdaptiveIntervalDays =
                            adaptiveConsumption > 0
                                ? 1 / adaptiveConsumption
                                : 0,

                        DaysUntilPurchase =
                            stockDaysUntilPurchase,

                        StockStatus =
                            GetStockStatus(
                                stockDaysUntilPurchase),

                        StatusClass =
                            GetStockClass(
                                stockDaysUntilPurchase),

                        Priority =
                            GetPriority(
                                stockDaysUntilPurchase),

                        LastAdjustmentType =
                            stockState.LastAdjustmentType
                    };

                model.StockItems.Add(stockViewModel);

                // =====================================================
                // COUNTS
                // =====================================================

                if (daysUntilPurchase <= 0)
                {
                    model.DueNowCount++;
                }
                else if (daysUntilPurchase <= 3)
                {
                    model.DueSoonCount++;
                }
                else if (daysUntilPurchase <= 7)
                {
                    model.UpcomingCount++;
                }

                if (stockDaysUntilPurchase <= 7)
                {
                    model.StockGettingLowCount++;
                }

                if (result.Anomaly)
                {
                    model.AnomalyCount++;
                }

                await CreateAlertsAsync(
                    userId,
                    householdId,
                    result,
                    productName);
            }

            await SyncShoppingListAsync(
                userId,
                householdId,
                model.PurchaseRecommendations);

            var notifications =
                await _notificationService
                    .GetNotificationsAsync(
                        userId,
                        householdId);

            model.Notifications =
                notifications
                    .Select(x =>
                        new SmartTrackNotificationViewModel
                        {
                            NotificationId =
                                x.NotificationId,

                            ProductName =
                                x.ProductName,

                            NotificationType =
                                x.NotificationType,

                            Message =
                                x.Message,

                            IsRead =
                                x.IsRead,

                            CreatedOn =
                                x.CreatedOn
                        })
                    .ToList();

            model.RecentPurchases =
                history
                    .OrderByDescending(x =>
                        ParseDate(x.PurchaseDate))
                    .Take(10)
                    .Select(x =>
                        new RecentPurchaseViewModel
                        {
                            Product =
                                x.ProductName,

                            Quantity =
                                x.Quantity,

                            PurchaseDate =
                                FormatDate(
                                    x.PurchaseDate),

                            UnitPrice =
                                x.UnitPrice,

                            TotalPrice =
                                x.TotalPrice,

                            UserId =
                                x.UserId
                        })
                    .ToList();

            return model;
        }

        private async Task SyncShoppingListAsync(
            string userId,
            Guid householdId,
            List<PurchaseRecommendationViewModel> recommendations)
        {
            if (recommendations == null ||
                recommendations.Count == 0)
            {
                return;
            }

            var shoppingList =
                await _context.ShoppingLists
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.Status == "ACTIVE");

            if (shoppingList == null)
            {
                shoppingList =
                    new ShoppingList
                    {
                        UserId = userId,
                        Status = "ACTIVE",
                        CreatedDate = DateTime.Now,
                        Items = new List<ShoppingListItem>()
                    };

                _context.ShoppingLists.Add(shoppingList);

                await _context.SaveChangesAsync();
            }

            foreach (var recommendation in recommendations)
            {
                if (string.IsNullOrWhiteSpace(
                    recommendation.Product))
                {
                    continue;
                }

                string product =
                    recommendation.Product.Trim();

                var existingItem =
                    shoppingList.Items
                        .FirstOrDefault(x =>
                            x.Product.Equals(
                                product,
                                StringComparison.OrdinalIgnoreCase));

                DateTime? expectedDate = null;

                if (!string.IsNullOrWhiteSpace(
                    recommendation.ExpectedPurchaseDate))
                {
                    if (DateTime.TryParse(
                        recommendation.ExpectedPurchaseDate,
                        out DateTime parsedDate))
                    {
                        expectedDate = parsedDate;
                    }
                }

                if (existingItem != null)
                {
                    if (!existingItem.IsPurchased)
                    {
                        existingItem.Quantity =
                            Convert.ToDecimal(
                                recommendation.LatestQuantity);

                        existingItem.Priority =
                            recommendation.Priority;

                        existingItem.RecommendationStatus =
                            recommendation.Status;

                        existingItem.ExpectedPurchaseDate =
                            expectedDate;

                        existingItem.DaysUntilPurchase =
                            recommendation.DaysUntilPurchase;
                    }

                    continue;
                }

                var newItem =
                    new ShoppingListItem
                    {
                        ShoppingListId =
                            shoppingList.Id,

                        Product =
                            product,

                        Quantity =
                            Convert.ToDecimal(
                                recommendation.LatestQuantity),

                        Priority =
                            recommendation.Priority,

                        RecommendationStatus =
                            recommendation.Status,

                        ExpectedPurchaseDate =
                            expectedDate,

                        DaysUntilPurchase =
                            recommendation.DaysUntilPurchase,

                        IsPurchased = false,

                        PurchasedDate = null
                    };

                _context.ShoppingListItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CreateAlertsAsync(
            string userId,
            Guid householdId,
            SmartTrackPredictionResponse result,
            string product)
        {
            int days =
                result.DaysUntilPurchase ?? 0;

            if (days <= 0)
            {
                await _notificationService
                    .CreateNotificationAsync(
                        userId,
                        householdId,
                        product,
                        "PURCHASE_DUE",
                        $"{product} should be purchased now.");
            }
            else if (days <= 3)
            {
                await _notificationService
                    .CreateNotificationAsync(
                        userId,
                        householdId,
                        product,
                        "PURCHASE_SOON",
                        $"{product} may need to be purchased in {days} days.");
            }
            else if (days <= 7)
            {
                await _notificationService
                    .CreateNotificationAsync(
                        userId,
                        householdId,
                        product,
                        "STOCK_LOW",
                        $"{product} is getting low and may need to be purchased within {days} days.");
            }

            if (result.Anomaly)
            {
                await _notificationService
                    .CreateNotificationAsync(
                        userId,
                        householdId,
                        product,
                        "ANOMALY",
                        $"An unusual purchasing pattern was detected for {product}.");
            }
        }

        private string GetPriority(int days)
        {
            if (days <= 0)
                return "HIGH";

            if (days <= 3)
                return "MEDIUM";

            if (days <= 7)
                return "LOW";

            return "NORMAL";
        }

        private string GetStockStatus(int days)
        {
            if (days <= 0)
                return "PURCHASE NOW";

            if (days <= 3)
                return "VERY LOW";

            if (days <= 7)
                return "GETTING LOW";

            return "OK";
        }

        private string GetStockClass(int days)
        {
            if (days <= 0)
                return "danger";

            if (days <= 3)
                return "warning";

            if (days <= 7)
                return "info";

            return "success";
        }

        private DateTime ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date))
            {
                return date;
            }

            return DateTime.MinValue;
        }

        private string FormatDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (DateTime.TryParse(
                value,
                out DateTime date))
            {
                return date.ToString("dd/MM/yyyy");
            }

            return value;
        }
    }
}