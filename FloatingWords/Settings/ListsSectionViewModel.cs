using System.Collections.ObjectModel;
using System.Globalization;
using FloatingWords.Application.Storage;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Dialogs;
using FloatingWords.Mvvm;

namespace FloatingWords.Settings;

public sealed class ListsSectionViewModel : ObservableObject
{
    private readonly VocabularyListManager _lists;
    private readonly IDataDirectory _dataDirectory;
    private readonly IDialogService _dialogs;

    private string? _statusMessage;

    public ListsSectionViewModel(VocabularyListManager lists, IDataDirectory dataDirectory, IDialogService dialogs)
    {
        _lists = lists ?? throw new ArgumentNullException(nameof(lists));
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

        RefreshCommand = new RelayCommand(Refresh);
        OpenFolderCommand = new RelayCommand(OpenFolder);
        SelectCommand = new RelayCommand(Select);
        RenameCommand = new RelayCommand(Rename);
        DeleteCommand = new RelayCommand(Delete);

        ReloadLists();
    }

    public ObservableCollection<VocabularyListViewModel> Lists { get; } = [];

    public RelayCommand RefreshCommand { get; }

    public RelayCommand OpenFolderCommand { get; }

    public RelayCommand SelectCommand { get; }

    public RelayCommand RenameCommand { get; }

    public RelayCommand DeleteCommand { get; }

    public string CountText => Lists.Count == 0
        ? "还没有词书"
        : string.Format(CultureInfo.InvariantCulture, "共 {0} 本词书", Lists.Count);

    public bool HasNoLists => Lists.Count == 0;

    public string StatusMessage
    {
        get => _statusMessage ?? string.Empty;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);

    public void ReloadLists()
    {
        string? currentId = _lists.CurrentListId;

        Lists.Clear();

        foreach (VocabularyListSummary summary in _lists.Lists)
        {
            Lists.Add(new VocabularyListViewModel(summary)
            {
                IsCurrent = string.Equals(summary.ListId, currentId, StringComparison.Ordinal)
            });
        }

        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(HasNoLists));
    }

    private void Refresh()
    {
        try
        {
            _lists.Refresh();
            ReloadLists();
            StatusMessage = "已刷新。";
        }
        catch (Exception exception)
        {
            _dialogs.ShowError("刷新词书", "刷新失败：" + exception.Message);
        }
    }

    private void OpenFolder()
    {
        try
        {
            _dataDirectory.OpenRootFolder();
        }
        catch (Exception exception)
        {
            _dialogs.ShowError("打开数据目录", "打开失败：" + exception.Message);
        }
    }

    private void Select(object? parameter)
    {
        if (parameter is not VocabularyListViewModel item)
        {
            return;
        }

        try
        {
            _lists.Select(item.ListId);
            ReloadLists();
            StatusMessage = $"已切换到「{item.Name}」。";
        }
        catch (Exception exception)
        {
            _dialogs.ShowError("切换词书", "切换失败：" + exception.Message);
        }
    }

    private void Rename(object? parameter)
    {
        if (parameter is not VocabularyListViewModel item)
        {
            return;
        }

        string? name = _dialogs.Prompt("重命名词书", "词书名称", item.Name);

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            _lists.Rename(item.ListId, name);
            ReloadLists();
            StatusMessage = "已重命名。";
        }
        catch (Exception exception)
        {
            _dialogs.ShowError("重命名词书", "重命名失败：" + exception.Message);
        }
    }

    private void Delete(object? parameter)
    {
        if (parameter is not VocabularyListViewModel item)
        {
            return;
        }

        bool confirmed = _dialogs.Confirm(
            "删除词书",
            $"确定删除「{item.Name}」吗？\n该词书的全部内容和评级都会被删除，且无法撤销。");

        if (!confirmed)
        {
            return;
        }

        try
        {
            _lists.Delete(item.ListId);
            ReloadLists();
            StatusMessage = "已删除。";
        }
        catch (Exception exception)
        {
            _dialogs.ShowError("删除词书", "删除失败：" + exception.Message);
        }
    }
}

public sealed class VocabularyListViewModel(VocabularyListSummary summary) : ObservableObject
{
    public string ListId => summary.ListId;

    public string Name => summary.ListName;

    public bool IsCurrent { get; set; }

    public string CountsText => string.Format(
        CultureInfo.InvariantCulture,
        "{0} 词 · {1} 义项",
        summary.EntryCount,
        summary.SenseCount);

    public string ListIdText => "listId: " + summary.ListId;

    public string SourceText => "来源：" + (summary.SourceName ?? summary.SourceFormat);

    public string ImportedLine => string.Format(
        CultureInfo.InvariantCulture,
        "导入于 {0:yyyy-MM-dd HH:mm}",
        summary.ImportedAt);
}
