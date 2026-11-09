using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Services;
using System.Text.Json;

namespace RaceDay.Part3.Controllers;

public class EventsController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var response = await api.GetAsync<JsonElement[]>("api/events", ct);
        if (!response.Success) TempData["Error"] = response.Message;
        return View(response.Data ?? Array.Empty<JsonElement>());
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var response = await api.GetAsync<JsonElement>($"api/events/{id}", ct);
        if (!response.Success || response.Data.ValueKind == JsonValueKind.Undefined)
        {
            TempData["Error"] = response.Message;
            return RedirectToAction(nameof(Index));
        }
        return View(response.Data);
    }
}
