using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModels;
using System.Net.Http.Headers;
using System.Text.Json;


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


            var viewModel = new OCRResponseModel
            {
                Items = result.Items,
                Date = result.Date, 
            };


            return View(viewModel);

        }


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



            return RedirectToAction("Index");
        }
    }

    }