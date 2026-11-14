using System.ComponentModel.DataAnnotations;

namespace ReliableAcademy.ViewModels;

public sealed class AssistantSettingsViewModel
{
    [StringLength(512)]
    public string? ApiKey { get; set; }

    public bool IsEnabled { get; set; }

    public bool HasApiKey { get; set; }
}

public sealed class AssistantChatRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Message { get; set; } = "";

    public List<AssistantChatHistoryItem> History { get; set; } = new();
}

public sealed class AssistantChatHistoryItem
{
    public string Role { get; set; } = "";
    [StringLength(2000)]
    public string Content { get; set; } = "";
}

public sealed record AssistantSettingsStatus(bool HasApiKey, bool IsEnabled);
public sealed record AssistantActionResult(bool Success, string Message);
