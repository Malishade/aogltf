namespace ConsoleUI;

public class ConsoleSpinner : IDisposable
{
    private readonly string[] _frames = { "|", "/", "-", "\\" };
    private readonly int _left;
    private readonly int _top;
    private readonly string _message;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _spinnerTask;
    private bool _disposed = false;

    private ConsoleSpinner(string message, int left, int top)
    {
        _message = message;
        _left = left;
        _top = top;
        _cancellationTokenSource = new CancellationTokenSource();

        _spinnerTask = Task.Run(() => Spin(_cancellationTokenSource.Token));
    }

    public static ConsoleSpinner Start(string message = "Loading...", int left = 2, int top = 2, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = color;
        Console.CursorVisible = false;
        return new ConsoleSpinner(message, left, top);
    }

    private void Spin(CancellationToken cancellationToken)
    {
        int frameIndex = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.SetCursorPosition(_left, _top);
                Console.Write($"{_frames[frameIndex]} {_message}");

                frameIndex = (frameIndex + 1) % _frames.Length;

                Thread.Sleep(80);
            }
            catch
            {
                break;
            }
        }
    }

    public void Stop(string? finalMessage = null, ConsoleColor? color = null)
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _spinnerTask.Wait();

            if (finalMessage != null)
            {
                Console.SetCursorPosition(_left, _top);
                var originalColor = Console.ForegroundColor;

                if (color.HasValue)
                    Console.ForegroundColor = color.Value;

                Console.Write(finalMessage.PadRight(_message.Length + 10));

                if (color.HasValue)
                    Console.ForegroundColor = originalColor;
            }

            Console.ResetColor();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}