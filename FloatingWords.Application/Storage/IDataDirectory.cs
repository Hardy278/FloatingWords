namespace FloatingWords.Application.Storage;

public interface IDataDirectory
{
    string RootPath { get; }

    string SettingsPath { get; }

    string DatabasePath { get; }

    string LogsPath { get; }

    void EnsureWritable();

    void OpenRootFolder();
}

public sealed class DataDirectoryException(string message, Exception? innerException = null)
    : Exception(message, innerException);
