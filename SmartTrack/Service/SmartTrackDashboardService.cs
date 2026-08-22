using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;

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
            _context = context;

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
            // -------------------------------------------------
            // 1. FIND USER HOUSEHOLD
            // -------------------------------------------------

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


            // -------------------------------------------------
            // 2. GET USER
            // -------------------------------------------------

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId);


            // -------------------------------------------------
            // 3. CREATE DASHBOARD MODEL
            // -------------------------------------------------

            var model =
                new SmartTrackDashboardViewModel
                {
                    UserName =
                        user?.UserName ?? "User"
                };


            // -------------------------------------------------
            // 4. GET HOUSEHOLD PURCHASE HISTORY
            // -------------------------------------------------

            var history =
                await _purchaseHistoryService
                    .GetHouseholdPurchaseHistoryAsync(
                        userId,
                        householdId);


            // -------------------------------------------------
            // 5. GET UNIQUE PRODUCTS
            // -------------------------------------------------

            var products =
                history
                    .Select(x => x.ProductName)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


            // =================================================
            // 6. PROCESS EACH PRODUCT
            // =================================================

            foreach (var product in products)
            {
                // ---------------------------------------------
                // GET PRODUCT HISTORY
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
                // CALL PYTHON SMARTTRACK AI
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
                    // Do not stop the entire dashboard
                    // if AI fails for one product.
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
                    string.IsNullOrWhiteSpace(result.Product)
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

                        // IMPORTANT:
                        // ViewModel expects STRING
                        LastPurchaseDate =
                            FormatDate(
                                result.LastPurchaseDate),

                        // IMPORTANT:
                        // ViewModel expects STRING
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
                    .Add(stock);


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


                // -------------------------------------------------
                // STOCK GETTING LOW
                // -------------------------------------------------

                if (daysUntilPurchase <= 7)
                {
                    model.StockGettingLowCount++;
                }


                // -------------------------------------------------
                // ANOMALY
                // -------------------------------------------------

                if (result.Anomaly)
                {
                    model.AnomalyCount++;
                }


                // =================================================
                // CREATE NOTIFICATIONS
                // =================================================

                await CreateAlertsAsync(
                    userId,
                    householdId,
                    result,
                    productName);
            }


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
            // RETURN DASHBOARD
            // =====================================================

            return model;
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


            // -------------------------------------------------
            // PURCHASE NOW
            // -------------------------------------------------

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


            // -------------------------------------------------
            // PURCHASE SOON
            // -------------------------------------------------

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


            // -------------------------------------------------
            // STOCK LOW
            // -------------------------------------------------

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


            // -------------------------------------------------
            // ANOMALY
            // -------------------------------------------------

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


        // =====================================================
        // STOCK STATUS
        // =====================================================

        private string GetStockStatus(int days)
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


        // =====================================================
        // STOCK CSS CLASS
        // =====================================================

        private string GetStockClass(int days)
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
                return date.ToString("dd/MM/yyyy");
            }

            return value;
        }
    }
}