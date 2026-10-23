using System.Globalization;
using System.Text;
using FloatingWords.Application.Diagnostics;

namespace FloatingWords.Infrastructure.Diagnostics;

public sealed class FileAppLogger(string logDirectory) : IAppLogger
{
    private readonly Lock _sync = new();
    private readonly string _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));

    public void Info(string message) => Write("INF", message, null);

    public void Warn(string message) => Write("WRN", message, null);

    public void LogError(string message, Exception? exception = null) => Write("ERR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(_logDirectory);

                DateTime now = DateTime.Now;
                string path = Path.Combine(_logDirectory, $"app-{now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log");

                var builder = new StringBuilder();
                builder.Append('[')
                    .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                    .Append("] [")
                    .Append(level)
                    .Append("] ")
                    .AppendLine(message);

                if (exception is not null)
                {
                    builder.AppendLine(exception.ToString());
                }

                File.AppendAllText(path, builder.ToString(), new UTF8Encoding(false));
            }
        }
        catch
        {
            // 日志失败不能影响应用主流程。
        }
    }
}
