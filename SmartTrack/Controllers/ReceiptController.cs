using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using SmartTrack.ViewModels;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace SmartTrack.Controllers
{
    public class ReceiptController : Controller
    {

        private readonly HttpClient _client;
        private readonly ApplicationDbContext _context;


        public ReceiptController(
            HttpClient client,
            ApplicationDbContext context)
        {
            _client = client;
            _context = context;
        }



        [HttpGet]
        public IActionResult Scan()
        {
            return View();
        }


        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        public async Task<IActionResult> Scan(IFormFile image)
        {

            if (image == null)
            {
                return View();
            }


            using var content = new MultipartFormDataContent();


            using var stream = image.OpenReadStream();


            var fileContent = new StreamContent(stream);


            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(image.ContentType);



            content.Add(
                fileContent,
                "image",
                image.FileName
            );



            // Send image to Flask API
            var response = await _client.PostAsync(
                "http://127.0.0.1:5000/scan",
                content
            );



            var jsonResult =
                await response.Content.ReadAsStringAsync();



            // Convert JSON response to model

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };


            var result =
                JsonSerializer.Deserialize<OCRResponseModel>(
                    jsonResult,
                    options
                );

            if (result == null)
            {
                return View();
            }

            var viewModel = new OCRResponseModel
            {
                Items = result.Items,
                Date = result.Date,
            };


            return View(viewModel);

        }


        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        public async Task<IActionResult> SaveReceipt(
        SaveReceiptViewModel model)
        {

            // Get logged-in user id from session
            var userId = HttpContext.Session.GetString("UserId");


            if (userId == null)
            {
                return RedirectToAction("Login");
            }



            // Count existing receipts for user

            int count = await _context.Receipts
                .Where(x => x.UserId == userId)
                .CountAsync();



            var receipt = new ReceiptModel
            {
                UserId = userId,

                PurchaseDate = model.Date ?? DateTime.Now,

                TotalAmount = model.Items.Sum(x => x.Price),

                CreatedOn = DateTime.Now,

                CreatedBy = userId
            };

            foreach (var item in model.Items)
            {
                receipt.ReceiptItems.Add(new ReceiptItemModel
                {
                    ItemName = item.Name,

                    Quantity = (int)item.Quantity,

                    Unit = item.Unit,

                    UnitPrice = item.UnitPrice,

                    TotalPrice = item.Price,

                    CreatedOn = DateTime.Now,

                    CreatedBy = userId
                });
            }



            _context.Receipts.Add(receipt);


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Receipt Added successfully!";
            return RedirectToAction(nameof(Scan));
        }

        [HttpGet]
        public async Task<IActionResult> ViewSavedReceipts()
        {
            // Get logged-in user ID
            string? userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // ---------------------------------------------------------
            // 1. Find household of logged-in user
            // ---------------------------------------------------------

            var userHousehold = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userHousehold == null)
            {
                return View(
                    new SmartTrack.ViewModels.SavedReceiptsViewModel()
                );
            }

            Guid householdId = userHousehold.HouseHoldId;

            // ---------------------------------------------------------
            // 2. Get household information
            // ---------------------------------------------------------

            var household = await _context.HouseHoldDetails
                .FirstOrDefaultAsync(x => x.HouseHoldId == householdId);

            string householdName = household?.HouseHoldName ?? "Household";
            // ---------------------------------------------------------
            // 3. Get all users belonging to this household
            // ---------------------------------------------------------

            var householdUserIds = await _context.UserHouseHoldDetails
                .Where(x => x.HouseHoldId == householdId)
                .Select(x => x.UserId)
                .ToListAsync();

            // ---------------------------------------------------------
            // 4. Get receipts created by household users
            // ---------------------------------------------------------

            var receipts = await _context.Receipts
                .Where(r => householdUserIds.Contains(r.CreatedBy))
                .Include(r => r.ReceiptItems)
                .OrderByDescending(r => r.PurchaseDate)
                .ToListAsync();

            // ---------------------------------------------------------
            // 5. Get user names
            // ---------------------------------------------------------

            var userIds = receipts
                .Select(r => r.CreatedBy)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrEmpty(u.UserName)
                        ? u.UserName
                        : u.Email
                );

            // ---------------------------------------------------------
            // 6. Create receipt list
            // ---------------------------------------------------------

            var receiptList = receipts
                .Select(r => new SmartTrack.ViewModels.ReceiptListViewModel
                {
                    Id = r.ReceiptId,

                    PurchaseDate = r.PurchaseDate,

                    TotalAmount = r.TotalAmount,

                    CreatedOn = r.CreatedOn,

                    CreatedByName =
                        !string.IsNullOrEmpty(r.CreatedBy) &&
                        users.ContainsKey(r.CreatedBy)
                            ? users[r.CreatedBy]
                            : "Unknown User",

                    ModifiedOn = r.ModifiedOn,

                    ModifiedByName =
                        !string.IsNullOrEmpty(r.ModifiedBy) &&
                        users.ContainsKey(r.ModifiedBy)
                            ? users[r.ModifiedBy]
                            : null,

                    ItemCount = r.ReceiptItems.Count
                })
                .ToList();

            // ---------------------------------------------------------
            // 7. Create receipt item list
            // ---------------------------------------------------------

            var receiptItems = receipts
                .SelectMany(r => r.ReceiptItems.Select(item =>
                    new SmartTrack.ViewModels.SavedReceiptItemViewModel
                    {
                        Id = item.ReceiptItemId,

                        ReceiptId = r.ReceiptId,

                        PurchaseDate = r.PurchaseDate,

                        ItemName = item.ItemName,

                        Quantity = item.Quantity,

                        Unit = item.Unit,

                        UnitPrice = item.UnitPrice,

                        TotalPrice = item.TotalPrice
                    }))
                .ToList();

            // ---------------------------------------------------------
            // 8. Create final page ViewModel
            // ---------------------------------------------------------

            var viewModel =
                new SmartTrack.ViewModels.SavedReceiptsViewModel
                {
                    HouseholdId = householdId.GetHashCode(),
                    HouseholdName = household?.HouseHoldName ?? "Household",
                    Receipts = receiptList,
                    ReceiptItems = receiptItems
                };

            // ---------------------------------------------------------
            // 9. Return View
            // ---------------------------------------------------------

            return View("ViewSavedReceipts", viewModel);
        }

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpGet]
        public async Task<IActionResult> EditReceipt(int id)
        {
            // Get logged-in user
            string? userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find receipt and its items
            var receipt = await _context.Receipts
                .Include(r => r.ReceiptItems)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound();
            }

            // Security:
            // Make sure this receipt belongs to the household
            // of the logged-in user.

            var userHousehold = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userHousehold == null)
            {
                return Forbid();
            }

            var householdUserIds = await _context.UserHouseHoldDetails
                .Where(x => x.HouseHoldId == userHousehold.HouseHoldId)
                .Select(x => x.UserId)
                .ToListAsync();

            if (!householdUserIds.Contains(receipt.CreatedBy))
            {
                return Forbid();
            }

            // Create edit ViewModel
            var model = new EditReceiptViewModel
            {
                ReceiptId = receipt.ReceiptId,

                PurchaseDate = receipt.PurchaseDate,

                TotalAmount = receipt.TotalAmount,

                Items = receipt.ReceiptItems
                    .Select(item => new EditReceiptItemViewModel
                    {
                        ReceiptItemId = item.ReceiptItemId,

                        ItemName = item.ItemName,

                        Quantity = item.Quantity,

                        Unit = item.Unit,

                        UnitPrice = item.UnitPrice,

                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            };

            return View(model);
        }

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceipt(EditReceiptViewModel model)
        {
            string? userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find receipt
            var receipt = await _context.Receipts
                .Include(r => r.ReceiptItems)
                .FirstOrDefaultAsync(r => r.ReceiptId == model.ReceiptId);

            if (receipt == null)
            {
                return NotFound();
            }

            // ---------------------------------------------------------
            // Check household access
            // ---------------------------------------------------------

            var userHousehold = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userHousehold == null)
            {
                return Forbid();
            }

            var householdUserIds = await _context.UserHouseHoldDetails
                .Where(x => x.HouseHoldId == userHousehold.HouseHoldId)
                .Select(x => x.UserId)
                .ToListAsync();

            if (!householdUserIds.Contains(receipt.CreatedBy))
            {
                return Forbid();
            }

            // ---------------------------------------------------------
            // Update receipt details
            // ---------------------------------------------------------

            receipt.PurchaseDate = model.PurchaseDate;

            // ---------------------------------------------------------
            // Update receipt items
            // ---------------------------------------------------------

            foreach (var itemModel in model.Items)
            {
                var existingItem = receipt.ReceiptItems
                    .FirstOrDefault(x =>
                        x.ReceiptItemId == itemModel.ReceiptItemId);

                if (existingItem != null)
                {
                    existingItem.ItemName = itemModel.ItemName;

                    existingItem.Quantity = (int)itemModel.Quantity;

                    existingItem.Unit = itemModel.Unit;

                    existingItem.UnitPrice = itemModel.UnitPrice;

                    existingItem.TotalPrice = itemModel.TotalPrice;
                }
            }

            // ---------------------------------------------------------
            // Recalculate receipt total
            // ---------------------------------------------------------

            receipt.TotalAmount = receipt.ReceiptItems
                .Sum(x => x.TotalPrice);

            // ---------------------------------------------------------
            // Modified information
            // ---------------------------------------------------------

            receipt.ModifiedOn = DateTime.Now;

            receipt.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receipt updated successfully!";

            return RedirectToAction(nameof(ViewSavedReceipts));
        }


        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            // Get logged-in user
            string? userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find receipt
            var receipt = await _context.Receipts
                .Include(r => r.ReceiptItems)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound();
            }

            // Find user's household
            var userHousehold = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userHousehold == null)
            {
                return Forbid();
            }

            // Get all users in same household
            var householdUserIds = await _context.UserHouseHoldDetails
                .Where(x => x.HouseHoldId == userHousehold.HouseHoldId)
                .Select(x => x.UserId)
                .ToListAsync();

            // Make sure receipt belongs to this household
            if (!householdUserIds.Contains(receipt.CreatedBy))
            {
                return Forbid();
            }

            // Delete receipt items
            if (receipt.ReceiptItems != null &&
                receipt.ReceiptItems.Any())
            {
                _context.ReceiptItems.RemoveRange(receipt.ReceiptItems);
            }

            // Delete receipt
            _context.Receipts.Remove(receipt);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receipt deleted successfully.";

            return RedirectToAction(nameof(ViewSavedReceipts));
        }

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpGet]
        public async Task<IActionResult> DeleteReceiptItem(
    int id,
    int receiptId)
        {
            string? userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find logged-in user's household
            var userHousehold = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (userHousehold == null)
            {
                return Forbid();
            }

            // Get household users
            var householdUserIds = await _context.UserHouseHoldDetails
                .Where(x => x.HouseHoldId == userHousehold.HouseHoldId)
                .Select(x => x.UserId)
                .ToListAsync();

            // Find receipt
            var receipt = await _context.Receipts
                .Include(r => r.ReceiptItems)
                .FirstOrDefaultAsync(r => r.ReceiptId == receiptId);

            if (receipt == null)
            {
                return NotFound();
            }

            // Check receipt belongs to household
            if (!householdUserIds.Contains(receipt.CreatedBy))
            {
                return Forbid();
            }

            // Find item
            var item = receipt.ReceiptItems
                .FirstOrDefault(x => x.ReceiptItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            // Delete item
            _context.ReceiptItems.Remove(item);

            // Recalculate receipt total
            receipt.TotalAmount = receipt.ReceiptItems
                .Where(x => x.ReceiptItemId != id)
                .Sum(x => x.TotalPrice);

            // Update modified details
            receipt.ModifiedOn = DateTime.Now;
            receipt.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receipt item deleted successfully.";

            return RedirectToAction(nameof(ViewSavedReceipts));

        }

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpGet]
        public IActionResult ManualEntry()
        {
            var model = new SaveReceiptViewModel
            {
                Date = DateTime.Now,
                Items = new List<ReceiptItemViewModel>()
            };

            // Start with one empty item
            model.Items.Add(new ReceiptItemViewModel());

            return View(model);
        }

        [Authorize(Roles = "FamilyMembers,HouseholdOwner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualEntry(
            SaveReceiptViewModel model)
        {
            string? userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one item."
                );

                return View(model);
            }

            // Remove empty items
            model.Items = model.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
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
            decimal totalAmount = model.Items.Sum(x => x.Price);

            var receipt = new ReceiptModel
            {
                UserId = userId,

                PurchaseDate = model.Date ?? DateTime.Now,

                TotalAmount = totalAmount,

                CreatedOn = DateTime.Now,

                CreatedBy = userId
            };

            // Add items
            foreach (var item in model.Items)
            {
                receipt.ReceiptItems.Add(
                    new ReceiptItemModel
                    {
                        ItemName = item.Name,

                        Quantity = (int)item.Quantity,

                        Unit = item.Unit,

                        UnitPrice = item.UnitPrice,

                        TotalPrice = item.Price,

                        CreatedOn = DateTime.Now,

                        CreatedBy = userId
                    }
                );
            }

            _context.Receipts.Add(receipt);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Receipt added successfully.";

            return RedirectToAction(
                nameof(ViewSavedReceipts)
            );
        }
    }
}