using System.Windows.Media;
using FloatingWords.Domain.Ratings;
using FloatingWords.Mvvm;
using FloatingWords.Visuals;

namespace FloatingWords.Reading;

public sealed class LabelChipViewModel(string text)
{
    public string Text { get; } = text;
}

public sealed class DefinitionDisplayViewModel(string primaryText, string? secondaryText)
{
    public string PrimaryText { get; } = primaryText;

    public string? SecondaryText { get; } = secondaryText;

    public bool HasSecondary => !string.IsNullOrWhiteSpace(SecondaryText);
}

public sealed class ExampleDisplayViewModel(string primaryText, string? secondaryText)
{
    public string PrimaryText { get; } = primaryText;

    public string? SecondaryText { get; } = secondaryText;

    public bool HasSecondary => !string.IsNullOrWhiteSpace(SecondaryText);
}

public sealed class SenseDisplayViewModel(
    string partOfSpeech,
    IReadOnlyList<DefinitionDisplayViewModel> definitions,
    IReadOnlyList<ExampleDisplayViewModel> examples)
{
    public string PartOfSpeech { get; } = partOfSpeech;

    public bool HasPartOfSpeech => !string.IsNullOrWhiteSpace(PartOfSpeech);

    public IReadOnlyList<DefinitionDisplayViewModel> Definitions { get; } = definitions;

    public IReadOnlyList<ExampleDisplayViewModel> Examples { get; } = examples;

    public bool HasExamples => Examples.Count > 0;
}

public sealed class RatingOptionViewModel(Rating rating) : ObservableObject
{
    public string Id => rating.Id;

    public string DisplayName => rating.DisplayName;

    public bool IsSelected
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(Background));
            OnPropertyChanged(nameof(BorderBrush));
            OnPropertyChanged(nameof(Foreground));
        }
    }

    public string DisplayText => IsSelected ? $"✓ {DisplayName}" : DisplayName;

    public Brush Background => UiBrushes.FromHex(rating.ColorHex, IsSelected ? (byte)0x2A : (byte)0x1F);

    public Brush BorderBrush => UiBrushes.FromHex(rating.ColorHex, IsSelected ? (byte)0xA0 : (byte)0x66);

    public Brush Foreground => UiBrushes.FromHex(IsSelected ? "#F4FFF9" : "#EDF2FA");
}
