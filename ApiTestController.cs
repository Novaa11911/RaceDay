using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Part3.Controllers
{
    public class ApiTestController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Events()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("RaceDayApi");

                using var response = await client.GetAsync("api/events");

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ApiStatus = $"API returned: {(int)response.StatusCode}";
                    ViewBag.ApiResponse = content;
                    return View();
                }

                ViewBag.ApiStatus = "API connection successful";
                ViewBag.ApiResponse = content;

                return View();
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ApiStatus = "Could not connect to the API";
                ViewBag.ApiResponse = ex.Message;

                return View();
            }
            catch (TaskCanceledException)
            {
                ViewBag.ApiStatus = "The API request timed out";
                ViewBag.ApiResponse = "Check that the API is running.";
                return View();
            }
        }
    }
}
