using Microsoft.AspNetCore.Mvc;
using RaceDay.Part3.Models;
using RaceDay.Part3.Services;
using System.Text.Json;

namespace RaceDay.Part3.Controllers;

public class AuthController(RaceDayApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<LoginViewModel, JsonElement>("api/auth/login", model, ct);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); return View(model); }
        var user = result.Data;
        if (user.ValueKind == JsonValueKind.Object)
        {
            var root = user;
            HttpContext.Session.SetString("UserName", $"{Get(root, "firstName")} {Get(root, "lastName")}".Trim());
            HttpContext.Session.SetString("UserRole", Get(root, "role"));
            HttpContext.Session.SetString("UserEmail", Get(root, "email"));
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<RegisterViewModel, JsonElement>("api/auth/register", model, ct);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); return View(model); }
        TempData["Success"] = "Your account was created. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await api.PostAsync<object, JsonElement>("api/auth/logout", new { }, ct);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private static string Get(JsonElement obj, string property) =>
        obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(property, out var value) ? value.ToString() : "";
}
