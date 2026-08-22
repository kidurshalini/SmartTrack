//using SmartTrack.Models;
//using SmartTrack.ViewModel;
//using System.Text;
//using System.Text.Json;

//namespace SmartTrack.Services
//{
//    public class SmartTrackAIService
//    {
//        private readonly HttpClient _httpClient;


//        // =====================================================
//        // CONSTRUCTOR
//        // =====================================================

//        public SmartTrackAIService(
//            HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }


//        // =====================================================
//        // SEND HOUSEHOLD HISTORY TO PYTHON SMARTTRACK
//        // =====================================================

//        public async Task<SmartTrackPredictionResponse>
//            PredictAsync(
//                string productName,
//                string? adjustment,
//                List<SmartTrackPurchaseHistoryDto>
//                    purchaseHistory)
//        {
//            // -------------------------------------------------
//            // PREPARE REQUEST
//            // -------------------------------------------------

//            var requestData = new
//            {
//                product_name = productName,

//                adjustment = adjustment,

//                purchase_history = purchaseHistory
//            };


//            // -------------------------------------------------
//            // SERIALIZE JSON
//            // -------------------------------------------------

//            var json =
//                JsonSerializer.Serialize(
//                    requestData,
//                    new JsonSerializerOptions
//                    {
//                        PropertyNamingPolicy =
//                            JsonNamingPolicy.CamelCase
//                    });


//            // -------------------------------------------------
//            // CREATE HTTP CONTENT
//            // -------------------------------------------------

//            var content =
//                new StringContent(
//                    json,
//                    Encoding.UTF8,
//                    "application/json");


//            // -------------------------------------------------
//            // CALL FLASK API
//            // -------------------------------------------------

//            var response =
//                await _httpClient.PostAsync(
//                    "api/smarttrack/predict",
//                    content);


//            // -------------------------------------------------
//            // READ RESPONSE
//            // -------------------------------------------------

//            var responseBody =
//                await response.Content
//                    .ReadAsStringAsync();


//            // -------------------------------------------------
//            // HANDLE ERROR
//            // -------------------------------------------------

//            if (!response.IsSuccessStatusCode)
//            {
//                throw new Exception(
//                    $"SmartTrack AI returned " +
//                    $"{(int)response.StatusCode}: " +
//                    $"{responseBody}");
//            }


//            // -------------------------------------------------
//            // DESERIALIZE
//            // -------------------------------------------------

//            var result =
//                JsonSerializer.Deserialize
//                <SmartTrackPredictionResponse>(
//                    responseBody,
//                    new JsonSerializerOptions
//                    {
//                        PropertyNameCaseInsensitive = true
//                    });


//            if (result == null)
//            {
//                throw new Exception(
//                    "Unable to deserialize SmartTrack AI response.");
//            }


//            return result;
//        }
//    }
//}
using SmartTrack.Models;
using SmartTrack.ViewModel;
using System.Text;
using System.Text.Json;

namespace SmartTrack.Services
{
    public class SmartTrackAIService
    {
        private readonly HttpClient _httpClient;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public SmartTrackAIService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        // =====================================================
        // SEND HOUSEHOLD HISTORY TO PYTHON SMARTTRACK
        // =====================================================

        public async Task<SmartTrackPredictionResponse>
            PredictAsync(
                string productName,
                string? adjustment,
                List<SmartTrackPurchaseHistoryDto>
                    purchaseHistory)
        {
            // -------------------------------------------------
            // PREPARE REQUEST
            // -------------------------------------------------

            var requestData = new
            {
                product_name = productName,

                adjustment = adjustment,

                purchase_history = purchaseHistory
            };


            // -------------------------------------------------
            // SERIALIZE JSON
            // -------------------------------------------------

            var json =
                JsonSerializer.Serialize(
                    requestData,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    });


            // -------------------------------------------------
            // CREATE HTTP CONTENT
            // -------------------------------------------------

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");


            // -------------------------------------------------
            // CALL FLASK API
            // -------------------------------------------------

            var response =
                await _httpClient.PostAsync(
                    "api/smarttrack/predict",
                    content);


            // -------------------------------------------------
            // READ RESPONSE
            // -------------------------------------------------

            var responseBody =
                await response.Content
                    .ReadAsStringAsync();


            // -------------------------------------------------
            // HANDLE ERROR
            // -------------------------------------------------

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"SmartTrack AI returned " +
                    $"{(int)response.StatusCode}: " +
                    $"{responseBody}");
            }


            // -------------------------------------------------
            // DESERIALIZE
            // -------------------------------------------------

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