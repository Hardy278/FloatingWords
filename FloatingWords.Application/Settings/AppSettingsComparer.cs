namespace FloatingWords.Application.Settings;

public static class AppSettingsComparer
{
    public static SettingsChangeKind GetChanges(AppSettings previous, AppSettings next)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);

        SettingsChangeKind changes = SettingsChangeKind.None;

        if (!string.Equals(previous.CurrentListId, next.CurrentListId, StringComparison.Ordinal))
        {
            changes |= SettingsChangeKind.CurrentBook;
        }

        if (previous.ReadingMode != next.ReadingMode ||
            previous.RotationIntervalSeconds != next.RotationIntervalSeconds ||
            previous.PauseOnHover != next.PauseOnHover ||
            previous.ShowExamples != next.ShowExamples)
        {
            changes |= SettingsChangeKind.Reading;
        }

        if (!previous.Window.Equals(next.Window))
        {
            changes |= SettingsChangeKind.Window;
        }

        if (!StyleMapsEqual(previous.StyleMap, next.StyleMap))
        {
            changes |= SettingsChangeKind.Style;
        }

        return changes;
    }

    private static bool StyleMapsEqual(Dictionary<string, TextStyle>? left, Dictionary<string, TextStyle>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, TextStyle> pair in left)
        {
            if (!right.TryGetValue(pair.Key, out TextStyle? other) || !pair.Value.Equals(other))
            {
                return false;
            }
        }
        return true;
    }
}
