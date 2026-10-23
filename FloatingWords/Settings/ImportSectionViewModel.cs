using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using FloatingWords.Application.Vocabulary;
using FloatingWords.Dialogs;
using FloatingWords.Mvvm;

namespace FloatingWords.Settings;

public sealed class ImportSectionViewModel : ObservableObject
{
    private readonly VocabularyListManager _lists;
    private readonly IDialogService _dialogs;

    private ImportDraft? _draft;
    private string _fileName = string.Empty;
    private string _fileSizeText = string.Empty;
    private string _sourcePath = string.Empty;
    private string _listName = string.Empty;
    private string _listIdText = string.Empty;
    private string _countsText = string.Empty;
    private string? _statusMessage;
    private bool _canImport;

    public ImportSectionViewModel(VocabularyListManager lists, IDialogService dialogs)
    {
        _lists = lists ?? throw new ArgumentNullException(nameof(lists));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

        ChooseFileCommand = new RelayCommand(ChooseFile);
        ImportCommand = new RelayCommand(Import);
    }

    public event EventHandler? Imported;

    public ObservableCollection<string> Issues { get; } = [];

    public RelayCommand ChooseFileCommand { get; }

    public RelayCommand ImportCommand { get; }

    public bool HasPreview => _draft is not null;

    public bool HasIssues => Issues.Count > 0;

    public string ImportButtonText => _draft?.Preview.Conflicts == true ? "覆盖导入" : "导入";

    public bool CanImport
    {
        get => _canImport;
        private set => SetProperty(ref _canImport, value);
    }

    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    public string FileSizeText
    {
        get => _fileSizeText;
        private set => SetProperty(ref _fileSizeText, value);
    }

    public string SourcePath
    {
        get => _sourcePath;
        private set => SetProperty(ref _sourcePath, value);
    }

    public string ListName
    {
        get => _listName;
        private set => SetProperty(ref _listName, value);
    }

    public string ListIdText
    {
        get => _listIdText;
        private set => SetProperty(ref _listIdText, value);
    }

    public string CountsText
    {
        get => _countsText;
        private set => SetProperty(ref _countsText, value);
    }

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

    private void ChooseFile()
    {
        string? path = _dialogs.PickJsonFile();

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            Inspect(path);
        }
        catch (Exception exception)
        {
            Reset();
            StatusMessage = "无法读取该文件：" + exception.Message;
            _dialogs.ShowError("导入词书", StatusMessage);
        }
    }

    private void Inspect(string path)
    {
        ImportDraft draft = _lists.InspectImport(path);
        ImportPreview preview = draft.Preview;

        _draft = draft;

        FileName = string.IsNullOrWhiteSpace(preview.FileName) ? Path.GetFileName(path) : preview.FileName;
        FileSizeText = FormatSize(preview.FileSize);
        SourcePath = preview.SourcePath;
        ListName = preview.ListName ?? "（未命名词书）";
        ListIdText = "listId: " + (preview.ListId ?? "—");
        CountsText = string.Format(
            CultureInfo.InvariantCulture,
            "{0} 词 · {1} 义项",
            preview.EntryCount,
            preview.SenseCount);

        Issues.Clear();

        foreach (ImportIssue issue in preview.Issues)
        {
            Issues.Add(issue.Line is null
                ? $"{issue.Path}：{issue.Message}"
                : string.Format(CultureInfo.InvariantCulture, "第 {0} 行 {1}：{2}", issue.Line, issue.Path, issue.Message));
        }

        StatusMessage = preview.IsValid ? string.Empty : "词书未通过校验，请先修正下列问题。";

        RaisePreviewChanged();
    }

    private void Import()
    {
        if (_draft is null)
        {
            return;
        }

        bool overwrite = false;

        if (_draft.Preview.Conflicts)
        {
            overwrite = _dialogs.Confirm(
                "覆盖导入",
                $"「{ListName}」已存在。\n覆盖后同名义项的评级会保留，被删除的义项评级会一并清除。是否继续？");

            if (!overwrite)
            {
                return;
            }
        }

        try
        {
            ImportResult result = _lists.Import(_draft, overwrite);
            StatusMessage = result.Message;

            if (result.Status == ImportStatus.Success)
            {
                Reset();
                Imported?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
        catch (Exception exception)
        {
            StatusMessage = "导入失败：" + exception.Message;
        }

        RaisePreviewChanged();
    }

    private void Reset()
    {
        _draft = null;

        FileName = string.Empty;
        FileSizeText = string.Empty;
        SourcePath = string.Empty;
        ListName = string.Empty;
        ListIdText = string.Empty;
        CountsText = string.Empty;

        Issues.Clear();
        RaisePreviewChanged();
    }

    private void RaisePreviewChanged()
    {
        CanImport = _draft?.Preview.IsValid == true;

        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(HasIssues));
        OnPropertyChanged(nameof(ImportButtonText));
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        <= 0 => "—",
        < 1024 => string.Format(CultureInfo.InvariantCulture, "{0} B", bytes),
        < 1024 * 1024 => string.Format(CultureInfo.InvariantCulture, "{0:0.#} KB", bytes / 1024d),
        _ => string.Format(CultureInfo.InvariantCulture, "{0:0.#} MB", bytes / (1024d * 1024d))
    };
}
