using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using System.Globalization;

namespace SmartTrack.Services
{
    public class SmartTrackDashboardService
    {
        private readonly ApplicationDbContext _context;

        private readonly SmartTrackPurchaseHistoryService
            _purchaseHistoryService;

        private readonly SmartTrackAIService
            _aiService;

        private readonly SmartTrackNotificationService
            _notificationService;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public SmartTrackDashboardService(
            ApplicationDbContext context,
            SmartTrackPurchaseHistoryService purchaseHistoryService,
            SmartTrackAIService aiService,
            SmartTrackNotificationService notificationService)
        {
            _context =
                context;

            _purchaseHistoryService =
                purchaseHistoryService;

            _aiService =
                aiService;

            _notificationService =
                notificationService;
        }


        // =====================================================
        // GET DASHBOARD
        // =====================================================

        public async Task<SmartTrackDashboardViewModel>
            GetDashboardAsync(string userId)
        {
            // =================================================
            // 1. FIND USER HOUSEHOLD
            // =================================================

            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId);


            if (userHousehold == null)
            {
                throw new Exception(
                    "User is not connected to a household.");
            }


            Guid householdId =
                userHousehold.HouseHoldId;


            // =================================================
            // 2. GET USER
            // =================================================

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId);


            // =================================================
            // 3. CREATE DASHBOARD MODEL
            // =================================================

            var model =
                new SmartTrackDashboardViewModel
                {
                    UserName =
                        user?.UserName ?? "User"
                };


            // =================================================
            // 4. GET PURCHASE HISTORY
            // =================================================

            var history =
                await _purchaseHistoryService
                    .GetHouseholdPurchaseHistoryAsync(
                        userId,
                        householdId);


            // =================================================
            // 5. GET UNIQUE PRODUCTS
            // =================================================

            var products =
                history
                    .Select(x => x.ProductName)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


            // =================================================
            // 6. PROCESS PRODUCTS
            // =================================================

            foreach (var product in products)
            {
                // ---------------------------------------------
                // PRODUCT HISTORY
                // ---------------------------------------------

                var productHistory =
                    history
                        .Where(x =>
                            string.Equals(
                                x.ProductName,
                                product,
                                StringComparison.OrdinalIgnoreCase))
                        .OrderBy(x =>
                            ParseDate(x.PurchaseDate))
                        .ToList();


                if (!productHistory.Any())
                {
                    continue;
                }


                // ---------------------------------------------
                // AI PREDICTION
                // ---------------------------------------------

                SmartTrackPredictionResponse? result;


                try
                {
                    result =
                        await _aiService.PredictAsync(
                            product,
                            "MEDIUM",
                            productHistory);
                }
                catch (Exception)
                {
                    continue;
                }


                if (result == null)
                {
                    continue;
                }


                // ---------------------------------------------
                // SAFE VALUES
                // ---------------------------------------------

                string productName =
                    string.IsNullOrWhiteSpace(
                        result.Product)
                            ? product
                            : result.Product;


                double latestQuantity =
                    result.LatestQuantity ?? 0;


                int daysUntilPurchase =
                    result.DaysUntilPurchase ?? 0;


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
                                result.LastPurchaseDate),

                        ExpectedPurchaseDate =
                            FormatDate(
                                result.ExpectedPurchaseDate),

                        DaysUntilPurchase =
                            daysUntilPurchase,

                        Status =
                            string.IsNullOrWhiteSpace(
                                result.Status)
                                    ? "NORMAL"
                                    : result.Status,

                        Recommendation =
                            result.Recommendation
                            ?? string.Empty,

                        Anomaly =
                            result.Anomaly,

                        AnomalyStatus =
                            result.AnomalyStatus
                            ?? "NORMAL",

                        AnomalyScore =
                            result.AnomalyScore ?? 0,

                        Priority =
                            GetPriority(
                                daysUntilPurchase)
                    };


                model.PurchaseRecommendations
                    .Add(recommendation);


                // =================================================
                // STOCK STATUS
                // =================================================

                var stock =
                    new StockStatusViewModel
                    {
                        Product =
                            productName,

                        LatestQuantity =
                            latestQuantity,

                        AdaptiveConsumption =
                            result.AdaptiveConsumption ?? 0,

                        AdaptiveIntervalDays =
                            result.AdaptiveIntervalDays ?? 0,

                        DaysUntilPurchase =
                            daysUntilPurchase,

                        StockStatus =
                            GetStockStatus(
                                daysUntilPurchase),

                        StatusClass =
                            GetStockClass(
                                daysUntilPurchase)
                    };


                model.StockItems
                    .Add(item: stock);


                // =================================================
                // COUNTS
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
                // STOCK LOW
                // =================================================

                if (daysUntilPurchase <= 7)
                {
                    model.StockGettingLowCount++;
                }


                // =================================================
                // ANOMALY
                // =================================================

                if (result.Anomaly)
                {
                    model.AnomalyCount++;
                }


                // =================================================
                // NOTIFICATIONS
                // =================================================

                await CreateAlertsAsync(
                    userId,
                    householdId,
                    result,
                    productName);
            }


            // =====================================================
            // IMPORTANT:
            // AUTOMATIC SHOPPING LIST CREATION
            // =====================================================

            await SyncShoppingListAsync(
                userId,
                householdId,
                model.PurchaseRecommendations);


            // =====================================================
            // GET NOTIFICATIONS
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
                    .ToList();


            // =====================================================
            // RECENT PURCHASES
            // =====================================================

            model.RecentPurchases =
                history
                    .OrderByDescending(x =>
                        ParseDate(
                            x.PurchaseDate))
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


            // =====================================================
            // RETURN
            // =====================================================

            return model;
        }


        // =====================================================
        // AUTOMATIC SHOPPING LIST SYNC
        // =====================================================

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


            // =================================================
            // FIND ACTIVE LIST
            // =================================================

            var shoppingList =
                await _context.ShoppingLists
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId &&
                        x.Status == "ACTIVE");


            // =================================================
            // CREATE LIST
            // =================================================

            if (shoppingList == null)
            {
                shoppingList =
                    new ShoppingList
                    {
                        UserId =
                            userId,

                        Status =
                            "ACTIVE",

                        CreatedDate =
                            DateTime.Now,

                        Items =
                            new List<ShoppingListItem>()
                    };


                _context.ShoppingLists
                    .Add(shoppingList);


                await _context.SaveChangesAsync();
            }


            // =================================================
            // SYNC RECOMMENDATIONS
            // =================================================

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


                // ---------------------------------------------
                // FIND EXISTING ITEM
                // ---------------------------------------------

                var existingItem =
                    shoppingList.Items
                        .FirstOrDefault(x =>
                            x.Product.Equals(
                                product,
                                StringComparison.OrdinalIgnoreCase));


                // ---------------------------------------------
                // EXPECTED DATE
                // ---------------------------------------------

                DateTime? expectedDate = null;


                if (!string.IsNullOrWhiteSpace(
                    recommendation.ExpectedPurchaseDate))
                {
                    if (DateTime.TryParse(
                        recommendation.ExpectedPurchaseDate,
                        out DateTime parsedDate))
                    {
                        expectedDate =
                            parsedDate;
                    }
                }


                // =============================================
                // EXISTING ITEM
                // =============================================

                if (existingItem != null)
                {
                    // -----------------------------------------
                    // Do not overwrite purchased items
                    // -----------------------------------------

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


                // =============================================
                // NEW ITEM
                // =============================================

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

                        IsPurchased =
                            false,

                        PurchasedDate =
                            null
                    };


                _context.ShoppingListItems
                    .Add(newItem);
            }


            // =================================================
            // SAVE
            // =================================================

            await _context.SaveChangesAsync();
        }


        // =====================================================
        // CREATE ALERTS
        // =====================================================

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


        // =====================================================
        // PRIORITY
        // =====================================================

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


        // =====================================================
        // STOCK STATUS
        // =====================================================

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


        // =====================================================
        // STOCK CLASS
        // =====================================================

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


        // =====================================================
        // PARSE DATE
        // =====================================================

        private DateTime ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }


            if (DateTime.TryParse(
                value,
                out DateTime date))
            {
                return date;
            }


            return DateTime.MinValue;
        }


        // =====================================================
        // FORMAT DATE
        // =====================================================

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
                return date.ToString(
                    "dd/MM/yyyy");
            }


            return value;
        }
    }
}