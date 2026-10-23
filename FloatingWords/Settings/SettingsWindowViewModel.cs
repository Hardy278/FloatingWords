using FloatingWords.Dialogs;
using FloatingWords.Mvvm;

namespace FloatingWords.Settings;

public sealed class SettingsWindowViewModel : ObservableObject, IDisposable
{
    private SettingsSectionItem _selectedSection;

    public SettingsWindowViewModel(AppHost host, IDialogService dialogs)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(dialogs);

        Sections =
        [
            new SettingsSectionItem("阅读", SettingsSection.Reading),
            new SettingsSectionItem("词书", SettingsSection.Lists),
            new SettingsSectionItem("导入", SettingsSection.Import),
            new SettingsSectionItem("样式", SettingsSection.Styles)
        ];

        Reading = new ReadingSectionViewModel(host.Settings);
        Lists = new ListsSectionViewModel(host.VocabularyLists, host.DataDirectory, dialogs);
        Import = new ImportSectionViewModel(host.VocabularyLists, dialogs);
        Styles = new StylesSectionViewModel(host.Settings);

        Import.Imported += OnImported;

        _selectedSection = Sections[0];
    }

    public IReadOnlyList<SettingsSectionItem> Sections { get; }

    public ReadingSectionViewModel Reading { get; }

    public ListsSectionViewModel Lists { get; }

    public ImportSectionViewModel Import { get; }

    public StylesSectionViewModel Styles { get; }

    public SettingsSectionItem SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (value is null || !SetProperty(ref _selectedSection, value))
            {
                return;
            }

            RaiseSelectionChanged();

            if (value.Section == SettingsSection.Lists)
            {
                Lists.ReloadLists();
            }
        }
    }

    public bool IsReadingSelected => _selectedSection.Section == SettingsSection.Reading;

    public bool IsListsSelected => _selectedSection.Section == SettingsSection.Lists;

    public bool IsImportSelected => _selectedSection.Section == SettingsSection.Import;

    public bool IsStylesSelected => _selectedSection.Section == SettingsSection.Styles;

    public void OpenSection(SettingsSection section)
    {
        SettingsSectionItem? match = Sections.FirstOrDefault(item => item.Section == section);

        if (match is not null)
        {
            SelectedSection = match;
        }
    }

    public void Dispose() => Import.Imported -= OnImported;

    private void OnImported(object? sender, EventArgs e) => Lists.ReloadLists();

    private void RaiseSelectionChanged()
    {
        OnPropertyChanged(nameof(IsReadingSelected));
        OnPropertyChanged(nameof(IsListsSelected));
        OnPropertyChanged(nameof(IsImportSelected));
        OnPropertyChanged(nameof(IsStylesSelected));
    }
}
