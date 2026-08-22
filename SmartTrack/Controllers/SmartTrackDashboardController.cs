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
        private readonly UserManager<ApplicationUser>
            _userManager;

        private readonly SmartTrackDashboardService
            _dashboardService;

        private readonly SmartTrackPurchaseHistoryService
            _purchaseHistoryService;

        private readonly SmartTrackAIService
            _aiService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SmartTrackDashboardController(
            UserManager<ApplicationUser> userManager,
            SmartTrackDashboardService dashboardService,
            SmartTrackPurchaseHistoryService purchaseHistoryService,
            SmartTrackAIService aiService)
        {
            _userManager =
                userManager;

            _dashboardService =
                dashboardService;

            _purchaseHistoryService =
                purchaseHistoryService;

            _aiService =
                aiService;
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

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


        // =========================================================
        // RECOMMENDATIONS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult>
            Recommendations()
        {
            var user =
                await _userManager
                    .GetUserAsync(User);

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


        // =========================================================
        // PREDICTION GET
        // =========================================================

        [HttpGet]
        public IActionResult Prediction()
        {
            return View(
                new SmartTrackPredictionViewModel());
        }


        // =========================================================
        // PREDICTION POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prediction(
     SmartTrackPredictionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ProductName))
            {
                ModelState.AddModelError(
                    nameof(model.ProductName),
                    "Please enter a product name.");

                return View(model);
            }

            try
            {
                // =====================================================
                // GET LOGGED-IN USER
                // =====================================================

                var user =
                    await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return Challenge();
                }


                // =====================================================
                // FIND USER HOUSEHOLD
                // =====================================================

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


                // =====================================================
                // GET HOUSEHOLD ID
                // =====================================================

                var householdId =
                    userHousehold.HouseHoldId;


                // =====================================================
                // GET PRODUCT HISTORY
                // =====================================================

                var productHistory =
                    await _purchaseHistoryService
                        .GetProductPurchaseHistoryAsync(
                            user.Id,
                            householdId,
                            model.ProductName.Trim());


                // =====================================================
                // CHECK HISTORY
                // =====================================================

                if (productHistory == null ||
                    productHistory.Count == 0)
                {
                    ModelState.AddModelError(
                        nameof(model.ProductName),
                        $"No purchase history was found for {model.ProductName} in your household.");

                    return View(model);
                }


                // =====================================================
                // CALL PYTHON AI SERVICE
                // =====================================================

                var prediction =
                    await _aiService.PredictAsync(
                        model.ProductName.Trim(),
                        "HIGH",
                        productHistory);


                // =====================================================
                // MAP BASIC RESULT
                // =====================================================

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
                // MAP PYTHON VALUES TO FRONTEND
                // =====================================================

                model.CurrentStock =
                    prediction.LatestQuantity ?? 0;

                model.AverageDailyUsage =
                    prediction.AdaptiveConsumption ?? 0;


                // =====================================================
                // LAST PURCHASE QUANTITY
                // =====================================================

                model.LastPurchaseQuantity =
                    prediction.LatestQuantity ?? 0;


                // =====================================================
                // DAYS SINCE LAST PURCHASE
                // =====================================================

                model.DaysSinceLastPurchase = 0;

                if (!string.IsNullOrWhiteSpace(
                    prediction.LastPurchaseDate))
                {
                    if (DateTime.TryParse(
                        prediction.LastPurchaseDate,
                        out DateTime lastPurchaseDate))
                    {
                        model.DaysSinceLastPurchase =
                            (int)Math.Max(
                                0,
                                (DateTime.Now.Date -
                                 lastPurchaseDate.Date).TotalDays);
                    }
                }


                // =====================================================
                // STATUS CSS
                // =====================================================

                if (model.PredictedDaysUntilPurchase <= 0)
                {
                    model.StatusClass = "danger";
                }
                else if (model.PredictedDaysUntilPurchase <= 3)
                {
                    model.StatusClass = "warning";
                }
                else if (model.PredictedDaysUntilPurchase <= 7)
                {
                    model.StatusClass = "info";
                }
                else
                {
                    model.StatusClass = "success";
                }


                // =====================================================
                // RETURN VIEW
                // =====================================================

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

        // =========================================================
        // DEBUG HOUSEHOLD
        // =========================================================

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

            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(new
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