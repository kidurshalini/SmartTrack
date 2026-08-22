using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using SmartTrack.ViewModels;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;

namespace SmartTrack.Controllers
{
    public class ReceiptController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory; // Change type to IHttpClientFactory
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReceiptController> _logger;

        public ReceiptController(
            IHttpClientFactory httpClientFactory, // Change parameter type
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<ReceiptController> logger)
        {
            _httpClientFactory = httpClientFactory; // Assign correctly
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }


        // =========================================================
        // SCAN RECEIPT - GET
        // =========================================================

        [HttpGet]
        public IActionResult Scan()
        {
            return View();
        }


        // =========================================================
        // SCAN RECEIPT - POST
        // =========================================================

       [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
[HttpPost]
public async Task<IActionResult> Scan(
    IFormFile image)
{
    try
    {
        // =====================================================
        // CHECK IMAGE
        // =====================================================

        if (image == null || image.Length == 0)
        {
            TempData["ErrorMessage"] =
                "Please select a receipt image.";

            return View();
        }


        // =====================================================
        // CREATE MULTIPART CONTENT
        // =====================================================

        using var content =
            new MultipartFormDataContent();


        using var stream =
            image.OpenReadStream();


        using var fileContent =
            new StreamContent(stream);


        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                image.ContentType
            );


        content.Add(
            fileContent,
            "image",
            image.FileName
        );


        // =====================================================
        // SEND TO OCR FLASK
        // =====================================================

        var client = _httpClientFactory.CreateClient();

        var response = await client.PostAsync(
            "http://127.0.0.1:5000/scan",
            content
        );


        // =====================================================
        // READ JSON
        // =====================================================

        var jsonResult =
            await response.Content.ReadAsStringAsync();


        Console.WriteLine(
            "\n================ OCR JSON ================\n"
        );

        Console.WriteLine(
            jsonResult
        );

        Console.WriteLine(
            "\n==========================================\n"
        );


        // =====================================================
        // CHECK HTTP ERROR
        // =====================================================

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] =
                "OCR server returned an error: "
                + jsonResult;

            return View();
        }


        // =====================================================
        // DESERIALIZE
        // =====================================================

        var options =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };


        var result =
            JsonSerializer.Deserialize<OCRResponseModel>(
                jsonResult,
                options
            );


        // =====================================================
        // CHECK RESULT
        // =====================================================

        if (result == null)
        {
            TempData["ErrorMessage"] =
                "Could not read OCR response.";

            return View();
        }


        // =====================================================
        // CHECK ITEMS
        // =====================================================

        if (result.Items == null)
        {
            result.Items =
                new List<ReceiptItemViewModel>();
        }


        // =====================================================
        // SHOW RESULT
        // =====================================================

        return View(
            result
        );
    }
    catch (HttpRequestException ex)
    {
        TempData["ErrorMessage"] =
            "Could not connect to OCR server. "
            + "Make sure Flask OCR is running on port 5000.";

        Console.WriteLine(
            "OCR CONNECTION ERROR: "
            + ex.Message
        );

        return View();
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] =
            "OCR processing failed: "
            + ex.Message;

        Console.WriteLine(
            "OCR ERROR: "
            + ex.Message
        );

        return View();
    }
}
        // =========================================================
        // SAVE OCR RECEIPT
        // =========================================================
        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        public async Task<IActionResult> SaveReceipt(
            SaveReceiptViewModel model)
        {
            var userId =
                HttpContext.Session.GetString("UserId");


            if (userId == null)
            {
                return RedirectToAction("Login");
            }


            if (model.Items == null ||
                !model.Items.Any())
            {
                TempData["ErrorMessage"] =
                    "No receipt items found.";

                return RedirectToAction(
                    nameof(Scan)
                );
            }


            var receipt = new ReceiptModel
            {
                UserId = userId,

                PurchaseDate =
                    model.Date ?? DateTime.Now,

                TotalAmount =
                    model.Items.Sum(
                        x => x.Price
                    ),

                CreatedOn =
                    DateTime.Now,

                CreatedBy =
                    userId
            };


            foreach (var item in model.Items)
            {
                receipt.ReceiptItems.Add(
                    new ReceiptItemModel
                    {
                        ItemName =
                            item.Name,

                        Quantity =
                            (int)item.Quantity,

                        Unit =
                            item.Unit,

                        UnitPrice =
                            item.UnitPrice,

                        TotalPrice =
                            item.Price,

                        CreatedOn =
                            DateTime.Now,

                        CreatedBy =
                            userId
                    }
                );
            }


            _context.Receipts.Add(
                receipt
            );


            await _context.SaveChangesAsync();


            // =====================================================
            // HOUSEHOLD EMAIL
            // =====================================================

            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );


            if (userHousehold != null)
            {
                await SendReceiptNotificationEmail(
                    userHousehold.HouseHoldId,
                    userId,
                    "Receipt Added",
                    "A new receipt has been added to your household.",
                    receipt
                );
            }


            TempData["SuccessMessage"] =
                "Receipt added successfully!";


            return RedirectToAction(
                nameof(Scan)
            );
        }


        // =========================================================
        // VIEW SAVED RECEIPTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ViewSavedReceipts()
        {
            // Get logged-in user ID
            string? userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // -----------------------------------------------------
            // 1. Find household of logged-in user
            // -----------------------------------------------------

            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );

            if (userHousehold == null)
            {
                return View(
                    new SmartTrack.ViewModels.SavedReceiptsViewModel()
                );
            }

            Guid householdId =
                userHousehold.HouseHoldId;


            // -----------------------------------------------------
            // 2. Get household information
            // -----------------------------------------------------

            var household =
                await _context.HouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.HouseHoldId == householdId
                    );

            string householdName =
                household?.HouseHoldName ?? "Household";


            // -----------------------------------------------------
            // 3. Get all users belonging to this household
            // -----------------------------------------------------

            var householdUserIds =
                await _context.UserHouseHoldDetails
                    .Where(
                        x => x.HouseHoldId == householdId
                    )
                    .Select(x => x.UserId)
                    .ToListAsync();


            // -----------------------------------------------------
            // 4. Get receipts created by household users
            // -----------------------------------------------------

            var receipts =
                await _context.Receipts
                    .Where(
                        r => householdUserIds.Contains(
                            r.CreatedBy
                        )
                    )
                    .Include(r => r.ReceiptItems)
                    .OrderByDescending(
                        r => r.PurchaseDate
                    )
                    .ToListAsync();


            // -----------------------------------------------------
            // 5. Get user names
            // -----------------------------------------------------

            var userIds =
                receipts
                    .Select(r => r.CreatedBy)
                    .Where(
                        x => !string.IsNullOrEmpty(x)
                    )
                    .Distinct()
                    .ToList();

            var users =
                await _context.Users
                    .Where(
                        u => userIds.Contains(u.Id)
                    )
                    .ToDictionaryAsync(
                        u => u.Id,
                        u =>
                            !string.IsNullOrEmpty(u.UserName)
                                ? u.UserName
                                : u.Email
                    );


            // -----------------------------------------------------
            // 6. Create receipt list
            // -----------------------------------------------------

            var receiptList =
                receipts
                    .Select(
                        r =>
                            new SmartTrack.ViewModels
                                .ReceiptListViewModel
                            {
                                Id = r.ReceiptId,

                                PurchaseDate =
                                    r.PurchaseDate,

                                TotalAmount =
                                    r.TotalAmount,

                                CreatedOn =
                                    r.CreatedOn,

                                CreatedByName =
                                    !string.IsNullOrEmpty(
                                        r.CreatedBy
                                    ) &&
                                    users.ContainsKey(
                                        r.CreatedBy
                                    )
                                        ? users[r.CreatedBy]
                                        : "Unknown User",

                                ModifiedOn =
                                    r.ModifiedOn,

                                ModifiedByName =
                                    !string.IsNullOrEmpty(
                                        r.ModifiedBy
                                    ) &&
                                    users.ContainsKey(
                                        r.ModifiedBy
                                    )
                                        ? users[r.ModifiedBy]
                                        : null,

                                ItemCount =
                                    r.ReceiptItems.Count
                            }
                    )
                    .ToList();


            // -----------------------------------------------------
            // 7. Create receipt item list
            // -----------------------------------------------------

            var receiptItems =
                receipts
                    .SelectMany(
                        r =>
                            r.ReceiptItems.Select(
                                item =>
                                    new SmartTrack.ViewModels
                                        .SavedReceiptItemViewModel
                                    {
                                        Id =
                                            item.ReceiptItemId,

                                        ReceiptId =
                                            r.ReceiptId,

                                        PurchaseDate =
                                            r.PurchaseDate,

                                        ItemName =
                                            item.ItemName,

                                        Quantity =
                                            item.Quantity,

                                        Unit =
                                            item.Unit,

                                        UnitPrice =
                                            item.UnitPrice,

                                        TotalPrice =
                                            item.TotalPrice
                                    }
                            )
                    )
                    .ToList();


            // -----------------------------------------------------
            // 8. Create final page ViewModel
            // -----------------------------------------------------

            var viewModel =
                new SmartTrack.ViewModels
                    .SavedReceiptsViewModel
                {
                    HouseholdId =
                        householdId.GetHashCode(),

                    HouseholdName =
                        household?.HouseHoldName
                        ?? "Household",

                    Receipts =
                        receiptList,

                    ReceiptItems =
                        receiptItems
                };


            // -----------------------------------------------------
            // 9. Return View
            // -----------------------------------------------------

            return View(
                "ViewSavedReceipts",
                viewModel
            );
        }


        // =========================================================
        // EDIT RECEIPT - GET
        // =========================================================

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpGet]
        public async Task<IActionResult> EditReceipt(int id)
        {
            // Get logged-in user
            string? userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // Find receipt and its items
            var receipt =
                await _context.Receipts
                    .Include(r => r.ReceiptItems)
                    .FirstOrDefaultAsync(
                        r => r.ReceiptId == id
                    );

            if (receipt == null)
            {
                return NotFound();
            }

            // Security:
            // Make sure this receipt belongs to the household
            // of the logged-in user.

            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );

            if (userHousehold == null)
            {
                return Forbid();
            }

            var householdUserIds =
                await _context.UserHouseHoldDetails
                    .Where(
                        x =>
                            x.HouseHoldId ==
                            userHousehold.HouseHoldId
                    )
                    .Select(x => x.UserId)
                    .ToListAsync();

            if (!householdUserIds.Contains(
                receipt.CreatedBy))
            {
                return Forbid();
            }

            // Create edit ViewModel
            var model =
                new EditReceiptViewModel
                {
                    ReceiptId =
                        receipt.ReceiptId,

                    PurchaseDate =
                        receipt.PurchaseDate,

                    TotalAmount =
                        receipt.TotalAmount,

                    Items =
                        receipt.ReceiptItems
                            .Select(
                                item =>
                                    new EditReceiptItemViewModel
                                    {
                                        ReceiptItemId =
                                            item.ReceiptItemId,

                                        ItemName =
                                            item.ItemName,

                                        Quantity =
                                            item.Quantity,

                                        Unit =
                                            item.Unit,

                                        UnitPrice =
                                            item.UnitPrice,

                                        TotalPrice =
                                            item.TotalPrice
                                    }
                            )
                            .ToList()
                };

            return View(model);
        }


        // =========================================================
        // EDIT RECEIPT - POST
        // =========================================================

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceipt(
            EditReceiptViewModel model)
        {
            string? userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find receipt
            var receipt =
                await _context.Receipts
                    .Include(r => r.ReceiptItems)
                    .FirstOrDefaultAsync(
                        r => r.ReceiptId == model.ReceiptId
                    );

            if (receipt == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Check household access
            // -----------------------------------------------------

            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );

            if (userHousehold == null)
            {
                return Forbid();
            }

            var householdUserIds =
                await _context.UserHouseHoldDetails
                    .Where(
                        x =>
                            x.HouseHoldId ==
                            userHousehold.HouseHoldId
                    )
                    .Select(x => x.UserId)
                    .ToListAsync();

            if (!householdUserIds.Contains(
                receipt.CreatedBy))
            {
                return Forbid();
            }

            // -----------------------------------------------------
            // Update receipt details
            // -----------------------------------------------------

            receipt.PurchaseDate =
                model.PurchaseDate;


            // -----------------------------------------------------
            // Update receipt items
            // -----------------------------------------------------

            foreach (var itemModel in model.Items)
            {
                var existingItem =
                    receipt.ReceiptItems
                        .FirstOrDefault(
                            x =>
                                x.ReceiptItemId ==
                                itemModel.ReceiptItemId
                        );

                if (existingItem != null)
                {
                    existingItem.ItemName =
                        itemModel.ItemName;

                    existingItem.Quantity =
                        (int)itemModel.Quantity;

                    existingItem.Unit =
                        itemModel.Unit;

                    existingItem.UnitPrice =
                        itemModel.UnitPrice;

                    existingItem.TotalPrice =
                        itemModel.TotalPrice;
                }
            }


            // -----------------------------------------------------
            // Recalculate receipt total
            // -----------------------------------------------------

            receipt.TotalAmount =
                receipt.ReceiptItems
                    .Sum(x => x.TotalPrice);


            // -----------------------------------------------------
            // Modified information
            // -----------------------------------------------------

            receipt.ModifiedOn =
                DateTime.Now;

            receipt.ModifiedBy =
                userId;


            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // SEND EMAIL
            // -----------------------------------------------------

            await SendReceiptNotificationEmail(
                userHousehold.HouseHoldId,
                userId,
                "Receipt Updated",
                "A receipt has been updated in your household.",
                receipt
            );


            TempData["SuccessMessage"] =
                "Receipt updated successfully!";

            return RedirectToAction(
                nameof(ViewSavedReceipts)
            );
        }


        // =========================================================
        // DELETE RECEIPT
        // =========================================================

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            // Get logged-in user
            string? userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // Find receipt
            var receipt =
                await _context.Receipts
                    .Include(r => r.ReceiptItems)
                    .FirstOrDefaultAsync(
                        r => r.ReceiptId == id
                    );

            if (receipt == null)
            {
                return NotFound();
            }

            // Find user's household
            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );

            if (userHousehold == null)
            {
                return Forbid();
            }

            // Get all users in same household
            var householdUserIds =
                await _context.UserHouseHoldDetails
                    .Where(
                        x =>
                            x.HouseHoldId ==
                            userHousehold.HouseHoldId
                    )
                    .Select(x => x.UserId)
                    .ToListAsync();

            // Make sure receipt belongs to this household
            if (!householdUserIds.Contains(
                receipt.CreatedBy))
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // SEND EMAIL BEFORE DELETE
            // -----------------------------------------------------

            await SendReceiptNotificationEmail(
                userHousehold.HouseHoldId,
                userId,
                "Receipt Deleted",
                "A receipt has been deleted from your household.",
                receipt
            );


            // Delete receipt items
            if (receipt.ReceiptItems != null &&
                receipt.ReceiptItems.Any())
            {
                _context.ReceiptItems.RemoveRange(
                    receipt.ReceiptItems
                );
            }

            // Delete receipt
            _context.Receipts.Remove(receipt);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receipt deleted successfully.";

            return RedirectToAction(
                nameof(ViewSavedReceipts)
            );
        }


        // =========================================================
        // DELETE RECEIPT ITEM
        // =========================================================

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpGet]
        public async Task<IActionResult> DeleteReceiptItem(
            int id,
            int receiptId)
        {
            string? userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            // Find logged-in user's household
            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );

            if (userHousehold == null)
            {
                return Forbid();
            }

            // Get household users
            var householdUserIds =
                await _context.UserHouseHoldDetails
                    .Where(
                        x =>
                            x.HouseHoldId ==
                            userHousehold.HouseHoldId
                    )
                    .Select(x => x.UserId)
                    .ToListAsync();

            // Find receipt
            var receipt =
                await _context.Receipts
                    .Include(r => r.ReceiptItems)
                    .FirstOrDefaultAsync(
                        r => r.ReceiptId == receiptId
                    );

            if (receipt == null)
            {
                return NotFound();
            }

            // Check receipt belongs to household
            if (!householdUserIds.Contains(
                receipt.CreatedBy))
            {
                return Forbid();
            }

            // Find item
            var item =
                receipt.ReceiptItems
                    .FirstOrDefault(
                        x =>
                            x.ReceiptItemId == id
                    );

            if (item == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // SEND EMAIL BEFORE DELETE
            // -----------------------------------------------------

            await SendReceiptItemNotificationEmail(
                userHousehold.HouseHoldId,
                userId,
                "Receipt Item Deleted",
                "An item has been deleted from a receipt in your household.",
                receipt,
                item
            );


            // Delete item
            _context.ReceiptItems.Remove(item);

            // Recalculate receipt total
            receipt.TotalAmount =
                receipt.ReceiptItems
                    .Where(
                        x =>
                            x.ReceiptItemId != id
                    )
                    .Sum(
                        x => x.TotalPrice
                    );

            // Update modified details
            receipt.ModifiedOn =
                DateTime.Now;

            receipt.ModifiedBy =
                userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receipt item deleted successfully.";

            return RedirectToAction(
                nameof(ViewSavedReceipts)
            );
        }


        // =========================================================
        // MANUAL ENTRY - GET
        // =========================================================

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpGet]
        public IActionResult ManualEntry()
        {
            var model =
                new SaveReceiptViewModel
                {
                    Date = DateTime.Now,

                    Items =
                        new List<ReceiptItemViewModel>()
                };

            // Start with one empty item
            model.Items.Add(
                new ReceiptItemViewModel()
            );

            return View(model);
        }


        // =========================================================
        // MANUAL ENTRY - POST
        // =========================================================

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualEntry(
            SaveReceiptViewModel model)
        {
            string? userId =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (model.Items == null ||
                !model.Items.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one item."
                );

                return View(model);
            }

            // Remove empty items
            model.Items =
                model.Items
                    .Where(
                        x =>
                            !string.IsNullOrWhiteSpace(
                                x.Name
                            )
                    )
                    .ToList();

            if (!model.Items.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Please enter at least one item."
                );

                return View(model);
            }

            // Calculate receipt total
            decimal totalAmount =
                model.Items.Sum(
                    x => x.Price
                );

            var receipt =
                new ReceiptModel
                {
                    UserId =
                        userId,

                    PurchaseDate =
                        model.Date ?? DateTime.Now,

                    TotalAmount =
                        totalAmount,

                    CreatedOn =
                        DateTime.Now,

                    CreatedBy =
                        userId
                };

            // Add items
            foreach (var item in model.Items)
            {
                receipt.ReceiptItems.Add(
                    new ReceiptItemModel
                    {
                        ItemName =
                            item.Name,

                        Quantity =
                            (int)item.Quantity,

                        Unit =
                            item.Unit,

                        UnitPrice =
                            item.UnitPrice,

                        TotalPrice =
                            item.Price,

                        CreatedOn =
                            DateTime.Now,

                        CreatedBy =
                            userId
                    }
                );
            }

            _context.Receipts.Add(receipt);

            await _context.SaveChangesAsync();


            // =====================================================
            // SEND EMAIL
            // =====================================================

            var userHousehold =
                await _context.UserHouseHoldDetails
                    .FirstOrDefaultAsync(
                        x => x.UserId == userId
                    );

            if (userHousehold != null)
            {
                await SendReceiptNotificationEmail(
                    userHousehold.HouseHoldId,
                    userId,
                    "Receipt Added",
                    "A new receipt has been manually added to your household.",
                    receipt
                );
            }


            TempData["SuccessMessage"] =
                "Receipt added successfully.";

            return RedirectToAction(
                nameof(ViewSavedReceipts)
            );
        }


        // =========================================================
        // SEND RECEIPT EMAIL TO HOUSEHOLD MEMBERS
        // =========================================================

        private async Task SendReceiptNotificationEmail(
            Guid householdId,
            string senderUserId,
            string subject,
            string actionMessage,
            ReceiptModel receipt)
        {
            try
            {
                // -------------------------------------------------
                // Get ONLY users belonging to this household
                // -------------------------------------------------

                var householdUserIds =
                    await _context.UserHouseHoldDetails
                        .Where(
                            x =>
                                x.HouseHoldId ==
                                householdId
                        )
                        .Select(
                            x => x.UserId
                        )
                        .ToListAsync();


                // -------------------------------------------------
                // Get users
                // -------------------------------------------------

                var users =
                    await _context.Users
                        .Where(
                            x =>
                                householdUserIds.Contains(
                                    x.Id
                                )
                        )
                        .ToListAsync();


                // -------------------------------------------------
                // Get household
                // -------------------------------------------------

                var household =
                    await _context.HouseHoldDetails
                        .FirstOrDefaultAsync(
                            x =>
                                x.HouseHoldId ==
                                householdId
                        );

                string householdName =
                    household?.HouseHoldName
                    ?? "Household";


                // -------------------------------------------------
                // Only HouseholdOwner + FamilyMembers
                // -------------------------------------------------

                foreach (var user in users)
                {
                    if (string.IsNullOrWhiteSpace(
                        user.Email))
                    {
                        continue;
                    }

                    var roles =
                        await _userManager.GetRolesAsync(
                            user
                        );

                    bool allowedRole =
                        roles.Contains(
                            "HouseholdOwner"
                        )
                        ||
                        roles.Contains(
                            "FamilyMembers"
                        );

                    if (!allowedRole)
                    {
                        continue;
                    }


                    // -------------------------------------------------
                    // Send email
                    // -------------------------------------------------

                    await SendReceiptEmail(
                        user.Email,
                        user.UserName ?? "User",
                        householdName,
                        subject,
                        actionMessage,
                        receipt
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending receipt notification for HouseholdId {HouseholdId}",
                    householdId
                );
            }
        }


        // =========================================================
        // SEND RECEIPT ITEM EMAIL
        // =========================================================

        private async Task SendReceiptItemNotificationEmail(
            Guid householdId,
            string senderUserId,
            string subject,
            string actionMessage,
            ReceiptModel receipt,
            ReceiptItemModel item)
        {
            try
            {
                // -------------------------------------------------
                // Get ONLY users from same household
                // -------------------------------------------------

                var householdUserIds =
                    await _context.UserHouseHoldDetails
                        .Where(
                            x =>
                                x.HouseHoldId ==
                                householdId
                        )
                        .Select(
                            x => x.UserId
                        )
                        .ToListAsync();


                // -------------------------------------------------
                // Get users
                // -------------------------------------------------

                var users =
                    await _context.Users
                        .Where(
                            x =>
                                householdUserIds.Contains(
                                    x.Id
                                )
                        )
                        .ToListAsync();


                // -------------------------------------------------
                // Get household
                // -------------------------------------------------

                var household =
                    await _context.HouseHoldDetails
                        .FirstOrDefaultAsync(
                            x =>
                                x.HouseHoldId ==
                                householdId
                        );

                string householdName =
                    household?.HouseHoldName
                    ?? "Household";


                // -------------------------------------------------
                // Only HouseholdOwner + FamilyMembers
                // -------------------------------------------------

                foreach (var user in users)
                {
                    if (string.IsNullOrWhiteSpace(
                        user.Email))
                    {
                        continue;
                    }

                    var roles =
                        await _userManager.GetRolesAsync(
                            user
                        );

                    bool allowedRole =
                        roles.Contains(
                            "HouseholdOwner"
                        )
                        ||
                        roles.Contains(
                            "FamilyMembers"
                        );

                    if (!allowedRole)
                    {
                        continue;
                    }


                    await SendReceiptItemEmail(
                        user.Email,
                        user.UserName ?? "User",
                        householdName,
                        subject,
                        actionMessage,
                        receipt,
                        item
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending receipt item notification for HouseholdId {HouseholdId}",
                    householdId
                );
            }
        }


        // =========================================================
        // ACTUAL RECEIPT EMAIL
        // =========================================================

        private async Task SendReceiptEmail(
            string email,
            string userName,
            string householdName,
            string subject,
            string actionMessage,
            ReceiptModel receipt)
        {
            try
            {
                string smtpUser =
                    _configuration[
                        "EmailSettings:SmtpUser"
                    ];

                string smtpPassword =
                    _configuration[
                        "EmailSettings:SmtpPassword"
                    ];


                // -------------------------------------------------
                // Create item rows
                // -------------------------------------------------

                string itemRows = "";

                foreach (var item in receipt.ReceiptItems)
                {
                    itemRows += $@"
<tr>

<td>
{WebUtility.HtmlEncode(item.ItemName)}
</td>

<td>
{item.Quantity}
</td>

<td>
{WebUtility.HtmlEncode(item.Unit ?? "-")}
</td>

<td>
Rs. {item.UnitPrice:N2}
</td>

<td>
Rs. {item.TotalPrice:N2}
</td>

</tr>";
                }


                // -------------------------------------------------
                // Email
                // -------------------------------------------------

                MailMessage message =
                    new MailMessage
                    {
                        From =
                            new MailAddress(
                                smtpUser,
                                "SmartTrack"
                            ),

                        Subject =
                            $"SmartTrack - {subject}",

                        IsBodyHtml =
                            true,

                        Body = $@"
<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<style>

body {{
    font-family: Arial, sans-serif;
    background-color: #f4f7f6;
    margin: 0;
    padding: 0;
}}

.container {{
    max-width: 700px;
    margin: 40px auto;
    background: white;
    border-radius: 10px;
    overflow: hidden;
    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
}}

.header {{
    background-color: #198754;
    color: white;
    text-align: center;
    padding: 25px;
}}

.content {{
    padding: 30px;
    color: #333;
}}

.info {{
    background-color: #f1f8f4;
    border-left: 5px solid #198754;
    padding: 15px;
    margin: 20px 0;
}}

table {{
    width: 100%;
    border-collapse: collapse;
    margin-top: 20px;
}}

th, td {{
    border: 1px solid #ddd;
    padding: 10px;
    text-align: left;
}}

th {{
    background-color: #198754;
    color: white;
}}

.total {{
    text-align: right;
    font-size: 18px;
    font-weight: bold;
    margin-top: 20px;
}}

.footer {{
    text-align: center;
    color: #777;
    padding: 20px;
    font-size: 14px;
}}

</style>

</head>

<body>

<div class='container'>

<div class='header'>

<h2>🌿 SmartTrack</h2>

<p>Household Receipt Notification</p>

</div>

<div class='content'>

<h3>
Hello {WebUtility.HtmlEncode(userName)},
</h3>

<p>
{actionMessage}
</p>

<div class='info'>

<p>
<strong>Household:</strong>
{WebUtility.HtmlEncode(householdName)}
</p>

<p>
<strong>Purchase Date:</strong>
{receipt.PurchaseDate:dd/MM/yyyy}
</p>

<p>
<strong>Receipt ID:</strong>
{receipt.ReceiptId}
</p>

</div>

<h4>Receipt Details</h4>

<table>

<thead>

<tr>

<th>Item</th>

<th>Quantity</th>

<th>Unit</th>

<th>Unit Price</th>

<th>Total</th>

</tr>

</thead>

<tbody>

{itemRows}

</tbody>

</table>

<div class='total'>

Total Amount:
Rs. {receipt.TotalAmount:N2}

</div>

<p style='margin-top:25px;'>

This notification was generated automatically
by the SmartTrack household management system.

</p>

</div>

<div class='footer'>

<strong>Thank you,</strong><br>

SmartTrack Team

</div>

</div>

</body>

</html>"
                    };


                message.To.Add(email);


                // -------------------------------------------------
                // Gmail SMTP
                // -------------------------------------------------

                using (SmtpClient smtp =
                    new SmtpClient())
                {
                    smtp.Host =
                        "smtp.gmail.com";

                    smtp.Port =
                        587;

                    smtp.EnableSsl =
                        true;

                    smtp.Timeout =
                        10000;

                    smtp.UseDefaultCredentials =
                        false;

                    smtp.Credentials =
                        new NetworkCredential(
                            smtpUser,
                            smtpPassword
                        );

                    await smtp.SendMailAsync(
                        message
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send receipt email to {Email}",
                    email
                );
            }
        }


        // =========================================================
        // ACTUAL RECEIPT ITEM EMAIL
        // =========================================================

        private async Task SendReceiptItemEmail(
            string email,
            string userName,
            string householdName,
            string subject,
            string actionMessage,
            ReceiptModel receipt,
            ReceiptItemModel item)
        {
            try
            {
                string smtpUser =
                    _configuration[
                        "EmailSettings:SmtpUser"
                    ];

                string smtpPassword =
                    _configuration[
                        "EmailSettings:SmtpPassword"
                    ];


                MailMessage message =
                    new MailMessage
                    {
                        From =
                            new MailAddress(
                                smtpUser,
                                "SmartTrack"
                            ),

                        Subject =
                            $"SmartTrack - {subject}",

                        IsBodyHtml =
                            true,

                        Body = $@"
<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<style>

body {{
    font-family: Arial, sans-serif;
    background-color: #f4f7f6;
    margin: 0;
    padding: 0;
}}

.container {{
    max-width: 600px;
    margin: 40px auto;
    background: white;
    border-radius: 10px;
    overflow: hidden;
    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
}}

.header {{
    background-color: #198754;
    color: white;
    text-align: center;
    padding: 25px;
}}

.content {{
    padding: 30px;
    color: #333;
}}

.info {{
    background-color: #f1f8f4;
    border-left: 5px solid #198754;
    padding: 15px;
    margin: 20px 0;
}}

.footer {{
    text-align: center;
    color: #777;
    padding: 20px;
}}

</style>

</head>

<body>

<div class='container'>

<div class='header'>

<h2>🌿 SmartTrack</h2>

<p>Receipt Item Notification</p>

</div>

<div class='content'>

<h3>
Hello {WebUtility.HtmlEncode(userName)},
</h3>

<p>
{actionMessage}
</p>

<div class='info'>

<p>
<strong>Household:</strong>
{WebUtility.HtmlEncode(householdName)}
</p>

<p>
<strong>Receipt ID:</strong>
{receipt.ReceiptId}
</p>

<p>
<strong>Purchase Date:</strong>
{receipt.PurchaseDate:dd/MM/yyyy}
</p>

</div>

<h4>Deleted Item</h4>

<div class='info'>

<p>
<strong>Item:</strong>
{WebUtility.HtmlEncode(item.ItemName)}
</p>

<p>
<strong>Quantity:</strong>
{item.Quantity}
</p>

<p>
<strong>Unit:</strong>
{WebUtility.HtmlEncode(item.Unit ?? "-")}
</p>

<p>
<strong>Unit Price:</strong>
Rs. {item.UnitPrice:N2}
</p>

<p>
<strong>Total Price:</strong>
Rs. {item.TotalPrice:N2}
</p>

</div>

<p>

This notification was generated automatically
by the SmartTrack household management system.

</p>

</div>

<div class='footer'>

<strong>Thank you,</strong><br>

SmartTrack Team

</div>

</div>

</body>

</html>"
                    };


                message.To.Add(email);


                using (SmtpClient smtp =
                    new SmtpClient())
                {
                    smtp.Host =
                        "smtp.gmail.com";

                    smtp.Port =
                        587;

                    smtp.EnableSsl =
                        true;

                    smtp.Timeout =
                        10000;

                    smtp.UseDefaultCredentials =
                        false;

                    smtp.Credentials =
                        new NetworkCredential(
                            smtpUser,
                            smtpPassword
                        );

                    await smtp.SendMailAsync(
                        message
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send receipt item email to {Email}",
                    email
                );
            }
        }
    }
}