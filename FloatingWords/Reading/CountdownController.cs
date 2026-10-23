using System.Diagnostics;
using System.Windows.Threading;

namespace FloatingWords.Reading;

[Flags]
public enum PauseReason
{
    None = 0,
    Hover = 1,
    Settings = 2,
    PersistenceError = 4
}

public sealed class CountdownController : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _watch = new();
    private TimeSpan _interval = TimeSpan.FromSeconds(5);
    private TimeSpan _remaining = TimeSpan.FromSeconds(5);
    private TimeSpan _remainingAtResume = TimeSpan.FromSeconds(5);
    private bool _started;
    private bool _elapsedRaised;
    private bool _disposed;

    public CountdownController()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += OnTick;
    }

    public event EventHandler? Tick;

    public event EventHandler? Elapsed;

    public bool IsRunning => _started && PauseReasons == PauseReason.None && !_elapsedRaised;

    public bool IsPaused => PauseReasons != PauseReason.None;

    public double Progress => _interval <= TimeSpan.Zero ? 0 : Math.Clamp(1 - _remaining.TotalMilliseconds / _interval.TotalMilliseconds, 0, 1);

    public int RemainingSeconds => Math.Max(0, (int)Math.Ceiling(_remaining.TotalSeconds));

    public PauseReason PauseReasons { get; private set; }

    public void Start(TimeSpan interval)
    {
        ThrowIfDisposed();

        _interval = interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : interval;
        _remaining = _interval;
        _remainingAtResume = _interval;
        PauseReasons = PauseReason.None;
        _started = true;
        _elapsedRaised = false;
        _watch.Restart();
        _timer.Start();
        RaiseTick();
    }

    public void Reset(TimeSpan interval)
    {
        ThrowIfDisposed();

        _interval = interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : interval;
        _remaining = _interval;
        _remainingAtResume = _interval;
        _elapsedRaised = false;

        if (_started && PauseReasons == PauseReason.None)
        {
            _watch.Restart();
            _timer.Start();
        }

        RaiseTick();
    }

    public void Pause(PauseReason reason)
    {
        if (reason == PauseReason.None || !_started)
        {
            return;
        }

        if (PauseReasons == PauseReason.None && !_elapsedRaised)
        {
            // 与 OnTick 用同一个绝对值公式重算。
            // 不能写成 _remaining -= _watch.Elapsed：_remaining 已经是“基准 − 已过时间”的
            // 绝对值，再减一次会把整段已过时间重复扣掉，表现为暂停瞬间进度条跳一大段。
            _remaining = _remainingAtResume - _watch.Elapsed;

            if (_remaining < TimeSpan.Zero)
            {
                _remaining = TimeSpan.Zero;
            }

            _watch.Reset();
        }

        PauseReasons |= reason;
        RaiseTick();
    }

    public void Resume(PauseReason reason)
    {
        if (reason == PauseReason.None)
        {
            return;
        }

        bool hadReason = (PauseReasons & reason) != 0;
        PauseReasons &= ~reason;

        if (hadReason && _started && PauseReasons == PauseReason.None && !_elapsedRaised)
        {
            // 秒表从零重新计时，剩余量必须以恢复瞬间的值为基准，
            // 否则 OnTick 会把进度算回整个区间（表现为恢复后进度条跳回开头）。
            _remainingAtResume = _remaining;
            _watch.Restart();
            _timer.Start();
        }

        RaiseTick();
    }

    public void Stop()
    {
        _timer.Stop();
        _watch.Reset();
        _started = false;
        _elapsedRaised = false;
        PauseReasons = PauseReason.None;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _watch.Reset();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_started || PauseReasons != PauseReason.None || _elapsedRaised)
        {
            return;
        }

        _remaining = _remainingAtResume - _watch.Elapsed;

        if (_remaining <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            _elapsedRaised = true;
            _timer.Stop();
            _watch.Stop();
            RaiseTick();
            Elapsed?.Invoke(this, EventArgs.Empty);
            return;
        }

        RaiseTick();
    }

    private void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
