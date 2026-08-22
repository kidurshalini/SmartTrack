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


        // =========================================================
        // MAIN DASHBOARD
        // =========================================================

        public async Task<SmartTrackDashboardViewModel>
            GetDashboardAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(
                    "User ID is required.",
                    nameof(userId));
            }


            // =====================================================
            // GET HOUSEHOLD
            // =====================================================

            var userHousehold =
                await _purchaseHistoryService
                    .GetUserHouseholdAsync(userId);

            if (userHousehold == null)
            {
                throw new Exception(
                    "User is not connected to a household.");
            }

            Guid householdId =
                userHousehold.HouseHoldId;


            // =====================================================
            // GET USER
            // =====================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId);


            var model =
                new SmartTrackDashboardViewModel
                {
                    UserName =
                        user?.UserName ?? "User"
                };


            // =====================================================
            // GET HOUSEHOLD PURCHASE HISTORY
            // =====================================================

            var history =
                await _purchaseHistoryService
                    .GetHouseholdPurchaseHistoryAsync(
                        userId,
                        householdId);


            // =====================================================
            // RECENT PURCHASES
            //
            // This is populated directly from SQL Server.
            // It should NOT depend on Python.
            // =====================================================

            model.RecentPurchases =
                history
                    .OrderByDescending(x =>
                        ParseDate(x.PurchaseDate))
                    .Take(10)
                    .Select(x =>
                        new RecentPurchaseViewModel
                        {
                            Product =
                                x.ProductName ?? string.Empty,

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
                                x.UserId ?? string.Empty
                        })
                    .ToList();


            // =====================================================
            // NO HISTORY
            // =====================================================

            if (history.Count == 0)
            {
                return model;
            }


            // =====================================================
            // GET DISTINCT PRODUCTS
            // =====================================================

            var products =
                history
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ProductName))
                    .Select(x =>
                        x.ProductName.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


            // =====================================================
            // PROCESS EACH PRODUCT
            // =====================================================

            foreach (var product in products)
            {
                var productHistory =
                    history
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.ProductName) &&
                            string.Equals(
                                x.ProductName.Trim(),
                                product,
                                StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x =>
                            ParseDate(x.PurchaseDate))
                        .ToList();


                if (productHistory.Count == 0)
                {
                    continue;
                }


                // =================================================
                // PYTHON AI PREDICTION
                // =================================================

                SmartTrackPredictionResponse prediction;

                try
                {
                    prediction =
                        await _aiService.PredictAsync(
                            product,
                            "MEDIUM",
                            productHistory);
                }
                catch (Exception ex)
                {
                    // IMPORTANT:
                    // Do not silently continue.
                    throw new Exception(
                        $"SmartTrack AI prediction failed for '{product}'. " +
                        $"Details: {ex.Message}",
                        ex);
                }


                if (prediction == null)
                {
                    throw new Exception(
                        $"SmartTrack AI returned no prediction for '{product}'.");
                }


                string productName =
                    string.IsNullOrWhiteSpace(
                        prediction.Product)
                            ? product
                            : prediction.Product.Trim();


                // =================================================
                // PYTHON VALUES
                // =================================================

                int daysUntilPurchase =
                    prediction.DaysUntilPurchase ?? 0;

                double latestQuantity =
                    prediction.LatestQuantity ?? 0;

                bool anomaly =
                    prediction.Anomaly;


                string status =
                    string.IsNullOrWhiteSpace(
                        prediction.Status)
                            ? GetStatusFromDays(
                                daysUntilPurchase)
                            : prediction.Status;


                string anomalyStatus =
                    string.IsNullOrWhiteSpace(
                        prediction.AnomalyStatus)
                            ? (anomaly
                                ? "ANOMALY"
                                : "NORMAL")
                            : prediction.AnomalyStatus;


                // =================================================
                // PURCHASE RECOMMENDATION
                // =================================================

                var recommendation =
                    new PurchaseRecommendationViewModel
                    {
                        Product =
                            productName,

                        LatestQuantity =
                            latestQuantity,

                        LastPurchaseDate =
                            FormatDate(
                                prediction.LastPurchaseDate),

                        ExpectedPurchaseDate =
                            FormatDate(
                                prediction.ExpectedPurchaseDate),

                        DaysUntilPurchase =
                            daysUntilPurchase,

                        Status =
                            status,

                        Recommendation =
                            prediction.Recommendation
                            ?? string.Empty,

                        Anomaly =
                            anomaly,

                        AnomalyStatus =
                            anomalyStatus,

                        AnomalyScore =
                            prediction.AnomalyScore ?? 0,

                        Priority =
                            GetPriority(
                                daysUntilPurchase),

                        NormalConsumption =
                            prediction.NormalConsumption ?? 0,

                        RecentConsumption =
                            prediction.RecentConsumption ?? 0,

                        AdaptiveConsumption =
                            prediction.AdaptiveConsumption ?? 0,

                        NormalIntervalDays =
                            prediction.NormalIntervalDays ?? 0,

                        RecentIntervalDays =
                            prediction.RecentIntervalDays ?? 0,

                        AdaptiveIntervalDays =
                            prediction.AdaptiveIntervalDays ?? 0,

                        Adjustment =
                            prediction.Adjustment ?? string.Empty,

                        AdjustmentFactor =
                            prediction.AdjustmentFactor ?? 0
                    };


                model.PurchaseRecommendations.Add(
                    recommendation);


                // =================================================
                // DASHBOARD COUNTS
                //
                // IMPORTANT:
                // These counts come directly from Python's
                // days_until_purchase.
                // =================================================

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


                // =================================================
                // ANOMALY COUNT
                // =================================================

                if (anomaly)
                {
                    model.AnomalyCount++;
                }


                // =================================================
                // STOCK PROCESSING
                //
                // Stock is separate from AI prediction.
                // If stock processing fails, the AI dashboard
                // values should still remain available.
                // =================================================

                try
                {
                    var stockState =
                        await _stockService
                            .ProcessStockAsync(
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
                        stockDaysUntilPurchase =
                            365;
                    }


                    stockDaysUntilPurchase =
                        Math.Max(
                            0,
                            Math.Min(
                                365,
                                stockDaysUntilPurchase));


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
                                    ? 1 /
                                      adaptiveConsumption
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
                                stockState
                                    .LastAdjustmentType
                                    ?? "NORMAL"
                        };


                    model.StockItems.Add(
                        stockViewModel);


                    if (stockDaysUntilPurchase <= 7)
                    {
                        model.StockGettingLowCount++;
                    }
                }
                catch
                {
                    // Stock calculation must not destroy
                    // the AI dashboard prediction.

                    model.StockItems.Add(
                        new StockStatusViewModel
                        {
                            Product =
                                productName,

                            LatestQuantity =
                                latestQuantity,

                            CurrentStock =
                                latestQuantity,

                            AdaptiveConsumption =
                                prediction
                                    .AdaptiveConsumption ?? 0,

                            AdaptiveIntervalDays =
                                prediction
                                    .AdaptiveIntervalDays ?? 0,

                            DaysUntilPurchase =
                                daysUntilPurchase,

                            StockStatus =
                                GetStockStatus(
                                    daysUntilPurchase),

                            StatusClass =
                                GetStockClass(
                                    daysUntilPurchase),

                            Priority =
                                GetPriority(
                                    daysUntilPurchase),

                            LastAdjustmentType =
                                prediction.Adjustment
                                ?? "NORMAL"
                        });
                }


                // =================================================
                // CREATE ALERTS
                // =================================================

                await CreateAlertsAsync(
                    userId,
                    householdId,
                    prediction,
                    productName);
            }


            // =====================================================
            // SHOPPING LIST
            // =====================================================

            await SyncShoppingListAsync(
                userId,
                householdId,
                model.PurchaseRecommendations);


            // =====================================================
            // NOTIFICATIONS
            // =====================================================

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
                    .OrderByDescending(x =>
                        x.CreatedOn)
                    .Take(10)
                    .ToList();


            return model;
        }


        // =========================================================
        // SHOPPING LIST
        // =========================================================

        private async Task SyncShoppingListAsync(
            string userId,
            Guid householdId,
            List<PurchaseRecommendationViewModel>
                recommendations)
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
                        Items =
                            new List<ShoppingListItem>()
                    };

                _context.ShoppingLists.Add(
                    shoppingList);

                await _context.SaveChangesAsync();
            }


            foreach (var recommendation
                     in recommendations)
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
                            x.Product != null &&
                            x.Product.Trim()
                                .Equals(
                                    product,
                                    StringComparison
                                        .OrdinalIgnoreCase));


                DateTime? expectedDate = null;


                if (!string.IsNullOrWhiteSpace(
                    recommendation.ExpectedPurchaseDate))
                {
                    if (DateTime.TryParse(
                        recommendation.ExpectedPurchaseDate,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime parsedDate))
                    {
                        expectedDate =
                            parsedDate;
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
                            recommendation
                                .DaysUntilPurchase;
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
                                recommendation
                                    .LatestQuantity),

                        Priority =
                            recommendation.Priority,

                        RecommendationStatus =
                            recommendation.Status,

                        ExpectedPurchaseDate =
                            expectedDate,

                        DaysUntilPurchase =
                            recommendation
                                .DaysUntilPurchase,

                        IsPurchased =
                            false,

                        PurchasedDate =
                            null
                    };


                _context.ShoppingListItems.Add(
                    newItem);
            }


            await _context.SaveChangesAsync();
        }


        // =========================================================
        // ALERTS
        // =========================================================

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


        // =========================================================
        // STATUS
        // =========================================================

        private string GetStatusFromDays(
            int days)
        {
            if (days <= 0)
            {
                return "DUE_NOW";
            }

            if (days <= 3)
            {
                return "DUE_SOON";
            }

            if (days <= 7)
            {
                return "UPCOMING";
            }

            return "NORMAL";
        }


        private string GetPriority(
            int days)
        {
            if (days <= 0)
            {
                return "HIGH";
            }

            if (days <= 3)
            {
                return "MEDIUM";
            }

            if (days <= 7)
            {
                return "LOW";
            }

            return "NORMAL";
        }


        // =========================================================
        // STOCK STATUS
        // =========================================================

        private string GetStockStatus(
            int days)
        {
            if (days <= 0)
            {
                return "PURCHASE NOW";
            }

            if (days <= 3)
            {
                return "VERY LOW";
            }

            if (days <= 7)
            {
                return "GETTING LOW";
            }

            return "OK";
        }


        private string GetStockClass(
            int days)
        {
            if (days <= 0)
            {
                return "danger";
            }

            if (days <= 3)
            {
                return "warning";
            }

            if (days <= 7)
            {
                return "info";
            }

            return "success";
        }


        // =========================================================
        // DATE
        // =========================================================

        private DateTime ParseDate(
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
                out DateTime date))
            {
                return date;
            }


            if (DateTime.TryParse(
                value,
                out date))
            {
                return date;
            }


            return DateTime.MinValue;
        }


        private string FormatDate(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }


            DateTime date =
                ParseDate(value);


            if (date == DateTime.MinValue)
            {
                return value;
            }


            return date.ToString(
                "dd/MM/yyyy");
        }
    }
}