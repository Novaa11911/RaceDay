using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Services;
using System.Text.Json;

namespace RaceDay.Part3.Controllers;

public class HomeController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var response = await api.GetAsync<JsonElement[]>("api/events", ct);
        ViewBag.ApiMessage = response.Success ? null : response.Message;
        return View(response.Data ?? Array.Empty<JsonElement>());
    }

    public IActionResult Error() => View();
}
