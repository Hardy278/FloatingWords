namespace FloatingWords.Application.Diagnostics;

public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void LogError(string message, Exception? exception = null);
}

public sealed class NullAppLogger : IAppLogger
{
    public static NullAppLogger Instance { get; } = new();

    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void LogError(string message, Exception? exception = null)
    {
    }
}
