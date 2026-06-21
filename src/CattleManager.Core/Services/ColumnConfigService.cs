using CattleManager.Core.Models;
using System.Text.Json;

namespace CattleManager.Core.Services;

public class ColumnConfigService
{
    private const string Key = "HerdListColumns";
    private readonly IAppSettingsRepository _settings;

    public ColumnConfigService(IAppSettingsRepository settings) => _settings = settings;

    public async Task<ColumnConfig> LoadAsync()
    {
        var json = await _settings.GetAsync(Key);
        if (json is null) return new ColumnConfig();
        try
        {
            return JsonSerializer.Deserialize<ColumnConfig>(json) ?? new ColumnConfig();
        }
        catch (JsonException)
        {
            return new ColumnConfig();
        }
    }

    public async Task SaveAsync(ColumnConfig config)
        => await _settings.SetAsync(Key, JsonSerializer.Serialize(config));
}
