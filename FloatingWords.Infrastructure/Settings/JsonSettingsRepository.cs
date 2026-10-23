using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FloatingWords.Application.Settings;

namespace FloatingWords.Infrastructure.Settings;

public sealed class JsonSettingsRepository(string settingsPath) : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly string _settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));

    public SettingsLoadResult Load()
    {
        if (!File.Exists(_settingsPath))
        {
            AppSettings defaults = SettingsDefaults.Create();
            Save(defaults);
            return new SettingsLoadResult(defaults, null);
        }

        try
        {
            string json = File.ReadAllText(_settingsPath, new UTF8Encoding(false, true));
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? throw new JsonException("设置文件内容为空。");
            SettingsDefaults.Normalize(settings);
            return new SettingsLoadResult(settings, null);
        }
        catch (Exception exception) when (exception is JsonException or DecoderFallbackException or IOException or UnauthorizedAccessException)
        {
            string warning = BackupAndReset(exception);
            return new SettingsLoadResult(SettingsDefaults.Create(), warning);
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            SettingsDefaults.Normalize(settings);

            string? directory = Path.GetDirectoryName(_settingsPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            string tempPath = _settingsPath + ".tmp";

            File.WriteAllText(tempPath, json, new UTF8Encoding(false));
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        catch (Exception exception)
        {
            throw new SettingsPersistenceException("保存设置失败。", exception);
        }
    }

    private string BackupAndReset(Exception exception)
    {
        string backupPath = Path.Combine(
            Path.GetDirectoryName(_settingsPath) ?? string.Empty,
            $"settings.invalid-{DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)}.json");

        try
        {
            File.Copy(_settingsPath, backupPath, overwrite: false);
        }
        catch
        {
            backupPath = string.Empty;
        }

        try
        {
            Save(SettingsDefaults.Create());
        }
        catch
        {
            // 已经处于恢复流程，仍然允许应用以默认设置启动。
        }

        string backupHint = string.IsNullOrEmpty(backupPath) ? "原设置文件备份失败。" : $"原设置文件已备份为 {Path.GetFileName(backupPath)}。";

        return $"设置文件损坏，已使用默认设置启动。{backupHint} 原因：{exception.Message}";
    }
}
