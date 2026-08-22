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
        private readonly SmartTrackAIService _smartTrackAI;

        private readonly SmartTrackPurchaseHistoryService
            _purchaseHistoryService;


        public SmartTrackController(
            SmartTrackAIService smartTrackAI,
            SmartTrackPurchaseHistoryService purchaseHistoryService)
        {
            _smartTrackAI = smartTrackAI;

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
                // 1. Get logged-in UserId
                // -------------------------------------------------

                var userId =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier
                    );

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        error = "User is not logged in."
                    });
                }


                // -------------------------------------------------
                // 2. Validate product
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(productName))
                {
                    return BadRequest(new
                    {
                        error =
                            "productName is required."
                    });
                }


                // -------------------------------------------------
                // 3. Get household purchase history
                // -------------------------------------------------

                var history =
                    await _purchaseHistoryService
                        .GetProductPurchaseHistoryAsync(
                            userId,
                            /* householdId: */ Guid.Empty, // or another valid Guid if you have it
                            productName
                        );


                // -------------------------------------------------
                // 4. No history
                // -------------------------------------------------

                if (history.Count == 0)
                {
                    return Ok(new
                    {
                        product = productName,

                        records_used = 0,

                        data_source = "SQL_SERVER",

                        status =
                            "NO_PURCHASE_HISTORY",

                        recommendation =
                            "No purchase history available."
                    });
                }


                // -------------------------------------------------
                // 5. Send household history to AI
                // -------------------------------------------------

                var result =
                    await _smartTrackAI.PredictAsync(
                        productName,
                        adjustment,
                        history
                    );


                // -------------------------------------------------
                // 6. Return prediction
                // -------------------------------------------------

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        error = ex.Message
                    }
                );
            }
        }
    }
}