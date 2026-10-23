using FloatingWords.Application.Diagnostics;
using FloatingWords.Application.Settings;
using FloatingWords.Domain.Vocabulary;

namespace FloatingWords.Application.Vocabulary;

public sealed class VocabularyListManager
{
    private readonly IVocabularyRepository _repository;
    private readonly SettingsService _settings;
    private readonly VocabularyImportService _importService;
    private readonly IAppLogger _logger;

    private IReadOnlyList<VocabularyListSummary> _lists = Array.Empty<VocabularyListSummary>();

    public VocabularyListManager(IVocabularyRepository repository, SettingsService settings,
        VocabularyImportService importService, IAppLogger? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _logger = logger ?? NullAppLogger.Instance;

        ReloadLists();
        EnsureCurrent();
    }

    public IReadOnlyList<VocabularyListSummary> Lists => _lists;

    public string? CurrentListId => _settings.Current.CurrentListId;

    public event EventHandler? ListsChanged;

    public event EventHandler? CurrentListChanged;

    public void Refresh()
    {
        ReloadLists();
        bool currentChanged = EnsureCurrent();

        ListsChanged?.Invoke(this, EventArgs.Empty);

        if (currentChanged)
        {
            CurrentListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public VocabularyList? LoadCurrent()
    {
        if (EnsureCurrent())
        {
            CurrentListChanged?.Invoke(this, EventArgs.Empty);
        }

        string? listId = CurrentListId;

        if (listId is null)
        {
            return null;
        }

        VocabularyList? list = _repository.Load(listId);

        if (list is not null)
        {
            return list;
        }

        _logger.Warn($"当前词书在数据库中不存在，尝试回退：{listId}。");
        ReloadLists();

        if (EnsureCurrent())
        {
            CurrentListChanged?.Invoke(this, EventArgs.Empty);
        }

        return CurrentListId is null ? null : _repository.Load(CurrentListId);
    }

    public void Select(string listId) => SelectInternal(listId, forceNotification: false);

    public void Rename(string listId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("词书名称不能为空。", nameof(newName));
        }

        newName = newName.Trim();

        if (newName.Length > 200)
        {
            throw new ArgumentException("词书名称不能超过 200 个字符。", nameof(newName));
        }

        _repository.Rename(listId, newName);
        _logger.Info($"已重命名词书：{listId} -> {newName}。");

        ReloadLists();
        ListsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Delete(string listId)
    {
        bool wasCurrent = string.Equals(CurrentListId, listId, StringComparison.Ordinal);

        _repository.Delete(listId);
        _logger.Info($"已删除词书：{listId}。");

        ReloadLists();
        bool currentChanged = EnsureCurrent();

        ListsChanged?.Invoke(this, EventArgs.Empty);

        if (wasCurrent || currentChanged)
        {
            CurrentListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ImportDraft InspectImport(string sourcePath) => _importService.Inspect(sourcePath);

    public ImportResult Import(ImportDraft draft, bool overwrite)
    {
        ImportResult result = _importService.Import(draft, overwrite);

        if (result.Status != ImportStatus.Success || result.ListId is null)
        {
            return result;
        }

        ReloadLists();
        SelectInternal(result.ListId, forceNotification: true);
        ListsChanged?.Invoke(this, EventArgs.Empty);

        return result;
    }

    private void SelectInternal(string listId, bool forceNotification)
    {
        if (!_lists.Any(list => string.Equals(list.ListId, listId, StringComparison.Ordinal)))
        {
            throw new VocabularyNotFoundException(listId);
        }

        string? currentId = CurrentListId;
        bool changed = !string.Equals(currentId, listId, StringComparison.Ordinal);

        if (changed)
        {
            _settings.Update(settings => settings.CurrentListId = listId);
        }

        if (changed || forceNotification)
        {
            CurrentListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ReloadLists() => _lists = _repository.GetSummaries();

    /// <summary>
    /// 确保当前词书 ID 指向一个存在的词书。返回是否发生了回退。
    /// </summary>
    private bool EnsureCurrent()
    {
        string? currentId = CurrentListId;

        if (_lists.Count == 0)
        {
            if (currentId is null)
            {
                return false;
            }

            _settings.Update(settings => settings.CurrentListId = null);
            return true;
        }

        if (currentId is not null && _lists.Any(list => string.Equals(list.ListId, currentId, StringComparison.Ordinal)))
        {
            return false;
        }

        string fallbackId = _lists[0].ListId;
        _settings.Update(settings => settings.CurrentListId = fallbackId);
        _logger.Warn(currentId is null ? $"未设置当前词书，回退到：{fallbackId}。" : $"当前词书不存在，回退到：{fallbackId}。");

        return true;
    }
}
