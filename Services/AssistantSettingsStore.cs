using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ReliableAcademy.Services;

public sealed class AssistantSettingsStore
{
    private readonly string _filePath;
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AssistantSettingsStore(IWebHostEnvironment environment, IDataProtectionProvider protectionProvider)
    {
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "AssistantSettings");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "settings.dat");
        _protector = protectionProvider.CreateProtector("ReliableAcademy.AdminAssistant.Settings.v1");
    }

    public async Task<StoredAssistantSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
                return new StoredAssistantSettings();

            var protectedValue = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(protectedValue))
                return new StoredAssistantSettings();

            var plainValue = _protector.Unprotect(protectedValue);
            return JsonSerializer.Deserialize<StoredAssistantSettings>(plainValue)
                ?? new StoredAssistantSettings();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(string apiKey, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var settings = new StoredAssistantSettings
        {
            ApiKey = apiKey,
            IsEnabled = isEnabled,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(settings);
        var protectedValue = _protector.Protect(json);
        var temporaryPath = _filePath + ".tmp";

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.WriteAllTextAsync(temporaryPath, protectedValue, cancellationToken);
            File.Move(temporaryPath, _filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            _gate.Release();
        }
    }
}

public sealed class StoredAssistantSettings
{
    public string ApiKey { get; set; } = "";
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
