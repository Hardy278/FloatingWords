using System.Windows;
using Microsoft.Win32;

namespace FloatingWords.Dialogs;

public sealed class WpfDialogService(Window owner) : IDialogService
{
    private readonly Window _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    public string? PickJsonFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入 JSON 词书",
            Filter = "JSON 词书 (*.json)|*.json",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    public string? Prompt(string title, string label, string initialValue)
    {
        var dialog = new PromptWindow(title, label, initialValue)
        {
            Owner = _owner
        };

        return dialog.ShowDialog() == true ? dialog.Value : null;
    }

    public bool Confirm(string title, string message)
        => MessageBox.Show(_owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void ShowError(string title, string message)
        => MessageBox.Show(_owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
