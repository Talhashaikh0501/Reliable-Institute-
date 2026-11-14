using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class AssistantController(
    AssistantSettingsStore settingsStore,
    AssistantAiService aiService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        ViewBag.HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey);
        ViewBag.AssistantEnabled = settings.IsEnabled;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var settings = await settingsStore.GetAsync(cancellationToken);
        return View(new AssistantSettingsViewModel
        {
            HasApiKey = !string.IsNullOrWhiteSpace(settings.ApiKey),
            IsEnabled = settings.IsEnabled
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(AssistantSettingsViewModel model, CancellationToken cancellationToken)
    {
        var current = await settingsStore.GetAsync(cancellationToken);
        var newKey = model.ApiKey?.Trim();
        var keyToSave = string.IsNullOrWhiteSpace(newKey) ? current.ApiKey : newKey;

        if (!string.IsNullOrWhiteSpace(newKey) &&
            (newKey.Length < 20 || newKey.Length > 512 || newKey.Any(char.IsWhiteSpace)))
        {
            ModelState.AddModelError(nameof(model.ApiKey),
                "Enter a valid API key without spaces. The key must be between 20 and 512 characters.");
        }

        if (string.IsNullOrWhiteSpace(keyToSave) && model.IsEnabled)
            ModelState.AddModelError(nameof(model.ApiKey), "Add an API key before enabling the assistant.");

        if (!ModelState.IsValid)
        {
            model.HasApiKey = !string.IsNullOrWhiteSpace(current.ApiKey);
            return View("Settings", model);
        }

        await settingsStore.SaveAsync(keyToSave ?? "", model.IsEnabled && !string.IsNullOrWhiteSpace(keyToSave), cancellationToken);
        TempData["AssistantSettingsSuccess"] = "Assistant settings saved securely.";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestConnection([FromForm] string? apiKey, CancellationToken cancellationToken)
    {
        var current = await settingsStore.GetAsync(cancellationToken);
        var candidate = string.IsNullOrWhiteSpace(apiKey) ? current.ApiKey : apiKey.Trim();

        if (string.IsNullOrWhiteSpace(candidate) ||
            candidate.Length < 20 || candidate.Length > 512 || candidate.Any(char.IsWhiteSpace))
        {
            return Json(new AssistantActionResult(false, "Enter a valid API key before testing the connection."));
        }

        var result = await aiService.TestConnectionAsync(candidate, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, message = "Enter a question before sending." });

        var settings = await settingsStore.GetAsync(cancellationToken);
        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(settings.ApiKey))
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                success = false,
                message = "The assistant is not configured yet. Open Assistant Settings to add an API key and enable it."
            });

        var safeHistory = (request.History ?? new List<AssistantChatHistoryItem>())
            .Where(x => x is not null && (x.Role == "user" || x.Role == "assistant") &&
                        !string.IsNullOrWhiteSpace(x.Content))
            .TakeLast(12)
            .Select(x => new AssistantChatHistoryItem
            {
                Role = x.Role,
                Content = x.Content.Length > 2000 ? x.Content[..2000] : x.Content
            })
            .ToList();

        try
        {
            var answer = await aiService.ChatAsync(
                settings.ApiKey,
                request.Message.Trim(),
                safeHistory,
                cancellationToken);

            return Json(new { success = true, answer });
        }
        catch (AssistantConnectionException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout,
                new { success = false, message = "The request was cancelled. Please try again." });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { success = false, message = "Something went wrong while preparing the answer. Please try again." });
        }
    }
}
