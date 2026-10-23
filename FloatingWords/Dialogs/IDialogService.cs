namespace FloatingWords.Dialogs;

public interface IDialogService
{
    string? PickJsonFile();

    string? Prompt(string title, string label, string initialValue);

    bool Confirm(string title, string message);

    void ShowError(string title, string message);
}
