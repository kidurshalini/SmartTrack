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
        private readonly SmartTrackDashboardService _dashboardService;
        private readonly SmartTrackPurchaseHistoryService _purchaseHistoryService;
        public SmartTrackDashboardController(
            UserManager<ApplicationUser> userManager,
            SmartTrackDashboardService dashboardService,
            SmartTrackPurchaseHistoryService purchaseHistoryService)
        {
            _userManager = userManager;
            _dashboardService = dashboardService;
            _purchaseHistoryService = purchaseHistoryService;
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var model =
                    await _dashboardService.GetDashboardAsync(user.Id);

                return View(model);
            }
            catch (Exception)
            {
                return View(
                    new SmartTrackDashboardViewModel
                    {
                        UserName = user.UserName ?? "User"
                    });
            }
        }

        // =====================================================
        // PURCHASE RECOMMENDATIONS
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Recommendations()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            try
            {
                var model =
                    await _dashboardService.GetDashboardAsync(user.Id);

                return View("Recommendations", model);
            }
            catch (Exception)
            {
                return View(
                    "Recommendations",
                    new SmartTrackDashboardViewModel
                    {
                        UserName = user.UserName ?? "User"
                    });
            }
        }

        // =====================================================
        // PREDICTION PAGE
        // =====================================================

        [HttpGet]
        public IActionResult Prediction()
        {
            return View(new SmartTrackPredictionViewModel());
        }

        // =====================================================
        // RUN PREDICTION
        // =====================================================

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
                // GET PURCHASE HISTORY FROM DATABASE
                // =====================================================

                var productHistory =
                    await _purchaseHistoryService
                        .GetProductHistoryAsync(model.ProductName);


                // =====================================================
                // CHECK WHETHER PRODUCT EXISTS
                // =====================================================

                if (productHistory == null ||
                    productHistory.Count == 0)
                {
                    ModelState.AddModelError(
                        nameof(model.ProductName),
                        $"No purchase history was found for {model.ProductName}.");

                    return View(model);
                }


                // =====================================================
                // SEND REAL HISTORY TO AI
                // =====================================================
                var prediction =
                    await _purchaseHistoryService.PredictAsync(
                        model.ProductName,
                        "MEDIUM",
                        productHistory);


                // =====================================================
                // MAP AI RESULT TO VIEW MODEL
                // =====================================================

                model.HasPrediction = true;

                model.PredictedDaysUntilPurchase =
                    prediction.PredictedDaysUntilPurchase;

                model.StockStatus =
                    prediction.StockStatus;

                model.Recommendation =
                    prediction.Recommendation;


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

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to generate the prediction.");

                return View(model);
            }
        }
    }
}
   
