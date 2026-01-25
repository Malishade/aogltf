public class ConsoleSpinner : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private readonly char[] _frames;
    private readonly string _message;
    private int _currentFrame;
    private Action<string> _updateAction;

    public ConsoleSpinner(string message, SpinnerStyle style = SpinnerStyle.Line, int intervalMs = 100)
    {
        _message = message;
        _frames = GetFrames(style);
        _currentFrame = 0;

        _timer = new System.Timers.Timer(intervalMs);
        _timer.Elapsed += (s, e) =>
        {
            _currentFrame = (_currentFrame + 1) % _frames.Length;
            _updateAction?.Invoke($"{_frames[_currentFrame]} {_message}");
        };
    }

    public void Start(Action<string> updateAction)
    {
        _updateAction = updateAction;
        _timer.Start();
        _updateAction?.Invoke($"{_frames[0]} {_message}");
    }

    public void Stop(string finalMessage = null)
    {
        _timer.Stop();
        if (finalMessage != null)
        {
            _updateAction?.Invoke(finalMessage);
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }

    private char[] GetFrames(SpinnerStyle style)
    {
        return style switch
        {
            SpinnerStyle.Line => new[] { '|', '/', '-', '\\' },
            SpinnerStyle.Dots => new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' },
            SpinnerStyle.Arrows => new[] { '←', '↖', '↑', '↗', '→', '↘', '↓', '↙' },
            SpinnerStyle.Circle => new[] { '◐', '◓', '◑', '◒' },
            SpinnerStyle.Bounce => new[] { '⠁', '⠂', '⠄', '⡀', '⢀', '⠠', '⠐', '⠈' },
            _ => new[] { '|', '/', '-', '\\' }
        };
    }
}

public enum SpinnerStyle
{
    Line,
    Dots,
    Arrows,
    Circle,
    Bounce
}