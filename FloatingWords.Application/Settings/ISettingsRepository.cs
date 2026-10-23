namespace FloatingWords.Application.Settings;

public interface ISettingsRepository
{
    SettingsLoadResult Load();

    void Save(AppSettings settings);
}
