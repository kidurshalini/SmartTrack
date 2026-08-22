using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrack.Models;
using SmartTrack.Services;

namespace SmartTrack.Controllers
{
    [Authorize]
    public class SmartTrackStockController
        : Controller
    {
        private readonly UserManager<ApplicationUser>
            _userManager;

        private readonly SmartTrackPurchaseHistoryService
            _purchaseHistoryService;

        private readonly SmartTrackStockService
            _stockService;

        public SmartTrackStockController(
            UserManager<ApplicationUser> userManager,
            SmartTrackPurchaseHistoryService purchaseHistoryService,
            SmartTrackStockService stockService)
        {
            _userManager =
                userManager;

            _purchaseHistoryService =
                purchaseHistoryService;

            _stockService =
                stockService;
        }

        // =========================================================
        // SAVE DAILY BEHAVIOUR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SetAdjustment(
                string productName,
                string adjustmentType,
                DateTime? date)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return BadRequest(
                    "Product name is required.");
            }

            var user =
                await _userManager
                    .GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var household =
                await _purchaseHistoryService
                    .GetUserHouseholdAsync(
                        user.Id);

            if (household == null)
            {
                return BadRequest(
                    "Household was not found.");
            }

            DateTime adjustmentDate =
                (date ?? DateTime.Today).Date;

            bool success =
                await _stockService
                    .SetDailyAdjustmentAsync(
                        user.Id,
                        household.HouseHoldId,
                        productName,
                        adjustmentDate,
                        adjustmentType);

            if (!success)
            {
                return BadRequest(
                    "Invalid stock adjustment.");
            }

            return RedirectToAction(
                "Index",
                "SmartTrackDashboard");
        }
    }
}