using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTrack.Services;

namespace SmartTrack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SmartTrackController : ControllerBase
    {
        private readonly SmartTrackAIService
            _smartTrackAI;

        private readonly SmartTrackPurchaseHistoryService
            _purchaseHistoryService;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public SmartTrackController(
            SmartTrackAIService smartTrackAI,
            SmartTrackPurchaseHistoryService
                purchaseHistoryService)
        {
            _smartTrackAI =
                smartTrackAI;

            _purchaseHistoryService =
                purchaseHistoryService;
        }


        // =========================================================
        // PREDICT
        // =========================================================

        [HttpGet("predict")]
        public async Task<IActionResult> Predict(
            [FromQuery] string productName,
            [FromQuery] string? adjustment = "MEDIUM")
        {
            try
            {
                // -------------------------------------------------
                // GET LOGGED-IN USER
                // -------------------------------------------------

                var userId =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        status =
                            "UNAUTHORIZED",

                        message =
                            "Logged-in user could not be identified."
                    });
                }


                // -------------------------------------------------
                // VALIDATE PRODUCT
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest(new
                    {
                        status =
                            "INVALID_PRODUCT",

                        message =
                            "productName is required."
                    });
                }


                productName =
                    productName.Trim();


                // -------------------------------------------------
                // GET HOUSEHOLD
                // -------------------------------------------------

                var householdId =
                    await _purchaseHistoryService
                        .GetHouseholdIdAsync(
                            userId);


                if (!householdId.HasValue ||
                    householdId.Value == Guid.Empty)
                {
                    return Ok(new
                    {
                        product =
                            productName,

                        records_used =
                            0,

                        data_source =
                            "SQL_SERVER",

                        status =
                            "HOUSEHOLD_NOT_FOUND",

                        recommendation =
                            "The logged-in user is not connected to a household."
                    });
                }


                // -------------------------------------------------
                // GET PRODUCT HISTORY
                // -------------------------------------------------

                var history =
                    await _purchaseHistoryService
                        .GetProductPurchaseHistoryAsync(
                            userId,
                            householdId.Value,
                            productName);


                // -------------------------------------------------
                // NO HISTORY
                // -------------------------------------------------

                if (history.Count == 0)
                {
                    return Ok(new
                    {
                        product =
                            productName,

                        records_used =
                            0,

                        data_source =
                            "SQL_SERVER",

                        household_id =
                            householdId.Value,

                        status =
                            "NO_PURCHASE_HISTORY",

                        recommendation =
                            "No purchase history available for this product in this household."
                    });
                }


                // -------------------------------------------------
                // SEND DATA TO PYTHON
                // -------------------------------------------------

                var result =
                    await _smartTrackAI
                        .PredictAsync(
                            productName,
                            adjustment,
                            history);


                // -------------------------------------------------
                // ADD INFORMATION
                // -------------------------------------------------

                result.Product =
                    productName;


                // -------------------------------------------------
                // RETURN
                // -------------------------------------------------

                return Ok(new
                {
                    product =
                        result.Product,

                    records_used =
                        history.Count,

                    data_source =
                        "SQL_SERVER",

                    prediction =
                        result
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(
                    503,
                    new
                    {
                        status =
                            "AI_SERVICE_UNAVAILABLE",

                        message =
                            "SmartTrack Python API is not available.",

                        details =
                            ex.Message
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        status =
                            "PREDICTION_ERROR",

                        message =
                            ex.Message
                    });
            }
        }


        // =========================================================
        // HOUSEHOLD HISTORY DEBUG
        // =========================================================

        [HttpGet("household-history")]
        public async Task<IActionResult>
            HouseholdHistory(
                [FromQuery] string productName)
        {
            try
            {
                // -------------------------------------------------
                // USER
                // -------------------------------------------------

                var userId =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Logged-in user could not be identified."
                    });
                }


                // -------------------------------------------------
                // PRODUCT
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest(new
                    {
                        message =
                            "productName is required."
                    });
                }


                // -------------------------------------------------
                // DEBUG DATA
                // -------------------------------------------------

                var debug =
                    await _purchaseHistoryService
                        .GetHouseholdDebugInfoAsync(
                            userId,
                            productName.Trim());


                return Ok(new
                {
                    logged_in_user =
                        debug.UserId,

                    household_id =
                        debug.HouseholdId,

                    household_users =
                        debug.HouseholdUserIds,

                    product =
                        debug.ProductName,

                    records_used =
                        debug.PurchaseHistory.Count,

                    data_source =
                        "SQL_SERVER",

                    history =
                        debug.PurchaseHistory
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        status =
                            "DEBUG_ERROR",

                        message =
                            ex.Message
                    });
            }
        }
    }
}