using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrack.Models;
using SmartTrack.Services;
using SmartTrack.ViewModels;

namespace SmartTrack.Controllers
{
    [Authorize]
    public class SmartTrackDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly SmartTrackDashboardService
            _dashboardService;

        private readonly SmartTrackPurchaseHistoryService
            _purchaseHistoryService;

        private readonly SmartTrackAIService
            _aiService;

        private readonly SmartTrackStockService
            _stockService;

        public SmartTrackDashboardController(
            UserManager<ApplicationUser> userManager,
            SmartTrackDashboardService dashboardService,
            SmartTrackPurchaseHistoryService purchaseHistoryService,
            SmartTrackAIService aiService,
            SmartTrackStockService stockService)
        {
            _userManager =
                userManager;

            _dashboardService =
                dashboardService;

            _purchaseHistoryService =
                purchaseHistoryService;

            _aiService =
                aiService;

            _stockService =
                stockService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var model =
                    await _dashboardService
                        .GetDashboardAsync(user.Id);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                return View(
                    new SmartTrackDashboardViewModel
                    {
                        UserName =
                            user.UserName ?? "User"
                    });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Recommendations()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var model =
                    await _dashboardService
                        .GetDashboardAsync(user.Id);

                return View(
                    "Recommendations",
                    model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                return View(
                    "Recommendations",
                    new SmartTrackDashboardViewModel
                    {
                        UserName =
                            user.UserName ?? "User"
                    });
            }
        }

        [HttpGet]
        public IActionResult Prediction()
        {
            return View(
                new SmartTrackPredictionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prediction(
            SmartTrackPredictionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(
                model.ProductName))
            {
                ModelState.AddModelError(
                    nameof(model.ProductName),
                    "Please enter a product name.");

                return View(model);
            }

            try
            {
                var user =
                    await _userManager
                        .GetUserAsync(User);

                if (user == null)
                {
                    return Challenge();
                }

                var userHousehold =
                    await _purchaseHistoryService
                        .GetUserHouseholdAsync(user.Id);

                if (userHousehold == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "You are not connected to a household.");

                    return View(model);
                }

                Guid householdId =
                    userHousehold.HouseHoldId;

                string productName =
                    model.ProductName.Trim();

                var productHistory =
                    await _purchaseHistoryService
                        .GetProductPurchaseHistoryAsync(
                            user.Id,
                            householdId,
                            productName);

                if (productHistory == null ||
                    productHistory.Count == 0)
                {
                    ModelState.AddModelError(
                        nameof(model.ProductName),
                        $"No purchase history was found for {productName} in your household.");

                    return View(model);
                }

                // =====================================================
                // REAL STOCK
                // =====================================================

                var stock =
                    await _stockService
                        .ProcessStockAsync(
                            user.Id,
                            householdId,
                            productName,
                            productHistory);

                // =====================================================
                // AI PREDICTION
                // =====================================================

                var prediction =
                    await _aiService.PredictAsync(
                        productName,
                        "HIGH",
                        productHistory);

                model.HasPrediction = true;

                model.PredictedDaysUntilPurchase =
                    prediction.DaysUntilPurchase ?? 0;

                model.StockStatus =
                    prediction.Status ?? "UNKNOWN";

                model.Recommendation =
                    prediction.Recommendation
                    ?? "No recommendation available.";

                // =====================================================
                // IMPORTANT:
                // USE REAL STOCK, NOT LATEST PURCHASE QUANTITY
                // =====================================================

                model.CurrentStock =
                    Convert.ToDouble(
                        stock.CurrentStock);

                model.AverageDailyUsage =
                    Convert.ToDouble(
                        stock.AdaptiveConsumption);

                model.LastPurchaseQuantity =
                    Convert.ToDouble(
                        stock.LastPurchaseQuantity);

                model.DaysSinceLastPurchase =
                    Math.Max(
                        0,
                        (
                            DateTime.Today -
                            stock.LastPurchaseDate.Date
                        ).Days);

                // =====================================================
                // STATUS
                // =====================================================

                if (stock.CurrentStock <= 0)
                {
                    model.StatusClass =
                        "danger";
                }
                else if (stock.AdaptiveConsumption > 0)
                {
                    double stockDays =
                        Convert.ToDouble(
                            stock.CurrentStock /
                            stock.AdaptiveConsumption);

                    if (stockDays <= 3)
                    {
                        model.StatusClass =
                            "warning";
                    }
                    else if (stockDays <= 7)
                    {
                        model.StatusClass =
                            "info";
                    }
                    else
                    {
                        model.StatusClass =
                            "success";
                    }
                }
                else
                {
                    model.StatusClass =
                        "success";
                }

                return View(model);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The SmartTrack AI service is unavailable: "
                    + ex.Message);

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to generate the prediction: "
                    + ex.Message);

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult>
            DebugHousehold(
                string productName)
        {
            var user =
                await _userManager
                    .GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(
                productName))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "productName is required."
                    });
            }

            var result =
                await _purchaseHistoryService
                    .GetHouseholdDebugInfoAsync(
                        user.Id,
                        productName);

            return Json(result);
        }
    }
}