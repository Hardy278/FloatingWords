using System.Diagnostics;
using FloatingWords.Application.Storage;

namespace FloatingWords.Infrastructure.Storage;

/// <summary>
/// 所有运行时数据都放在程序目录的 data 文件夹中。
/// </summary>
public sealed class PortableDataDirectory : IDataDirectory
{
    public PortableDataDirectory(string? programDirectory = null)
    {
        string? directory = programDirectory;

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.GetDirectoryName(Environment.ProcessPath);
        }

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = AppContext.BaseDirectory;
        }

        RootPath = Path.Combine(directory, "data");
        SettingsPath = Path.Combine(RootPath, "settings.json");
        DatabasePath = Path.Combine(RootPath, "floatingwords.db");
        LogsPath = Path.Combine(RootPath, "logs");
    }

    public string RootPath { get; }

    public string SettingsPath { get; }

    public string DatabasePath { get; }

    public string LogsPath { get; }

    public void EnsureWritable()
    {
        try
        {
            Directory.CreateDirectory(RootPath);
            Directory.CreateDirectory(LogsPath);

            string probePath = Path.Combine(RootPath, $".write-test-{Guid.NewGuid():N}");
            using (File.Create(probePath))
            {
            }

            File.Delete(probePath);
        }
        catch (Exception exception)
        {
            throw new DataDirectoryException(
                $"数据目录不可写：{RootPath}。请将程序安装到用户可写目录，" +
                "或让安装程序为该目录授予当前用户写入权限。",
                exception);
        }
    }

    public void OpenRootFolder()
    {
        Directory.CreateDirectory(RootPath);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RootPath,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            throw new DataDirectoryException($"无法打开数据目录：{RootPath}。", exception);
        }
    }
}
