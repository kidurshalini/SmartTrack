using SmartTrack.Models;
using System.Text;
using System.Text.Json;

namespace SmartTrack.Services
{
    public class SmartTrackAIService
    {
        private readonly HttpClient _httpClient;

        public SmartTrackAIService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        // =========================================================
        // SEND PURCHASE HISTORY TO PYTHON
        // =========================================================

        public async Task<SmartTrackPredictionResponse>
            PredictAsync(
                string productName,
                string? adjustment,
                List<SmartTrackPurchaseHistoryDto>
                    purchaseHistory)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new ArgumentException(
                    "Product name is required.",
                    nameof(productName));
            }

            if (purchaseHistory == null ||
                purchaseHistory.Count == 0)
            {
                throw new ArgumentException(
                    "Purchase history is required.",
                    nameof(purchaseHistory));
            }


            // -----------------------------------------------------
            // REQUEST
            // -----------------------------------------------------

            var requestData = new
            {
                product_name = productName.Trim(),

                adjustment =
                    string.IsNullOrWhiteSpace(adjustment)
                        ? "MEDIUM"
                        : adjustment,

                purchase_history =
                    purchaseHistory
            };


            // -----------------------------------------------------
            // JSON
            // -----------------------------------------------------

            var json =
                JsonSerializer.Serialize(
                    requestData,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    });


            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");


            // -----------------------------------------------------
            // PYTHON API
            // -----------------------------------------------------

            var response =
                await _httpClient.PostAsync(
                    "api/smarttrack/predict",
                    content);


            // -----------------------------------------------------
            // RESPONSE BODY
            // -----------------------------------------------------

            var responseBody =
                await response.Content
                    .ReadAsStringAsync();


            // -----------------------------------------------------
            // ERROR
            // -----------------------------------------------------

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"SmartTrack AI returned " +
                    $"{(int)response.StatusCode}: " +
                    $"{responseBody}");
            }


            // -----------------------------------------------------
            // DESERIALIZE
            // -----------------------------------------------------

            var result =
                JsonSerializer.Deserialize
                <SmartTrackPredictionResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });


            if (result == null)
            {
                throw new Exception(
                    "Unable to deserialize SmartTrack AI response.");
            }


            return result;
        }
    }
}