namespace FloatingWords.Settings;

public enum SettingsSection
{
    Reading,
    Lists,
    Import,
    Styles
}

public sealed record SettingsSectionItem(string Title, SettingsSection Section);
