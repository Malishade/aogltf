namespace ConsoleUI;

public class ConsoleLoadingMenu
{
    private readonly string _titleText;
    private readonly string _loadingText;
    private int _borderWidth = 80;
    private Action<string>? _successAction;
    private Action<string>? _errorAction;
    private Func<string, string>? _successMessageFunc;
    private Func<string, string>? _errorMessageFunc;
    private string _defaultSuccessMessage = "Operation completed successfully";
    private string _defaultErrorMessage = "Operation failed";
    private bool _waitForKey = true;

    private ConsoleLoadingMenu(string titleText, string loadingText)
    {
        _titleText = titleText;
        _loadingText = loadingText;
    }

    public static ConsoleLoadingMenu Create(string titleText, string loadingText)
    {
        return new ConsoleLoadingMenu(titleText, loadingText);
    }

    public ConsoleLoadingMenu WithBorderWidth(int width)
    {
        _borderWidth = width;
        return this;
    }

    public ConsoleLoadingMenu OnSuccess(Action<string> action)
    {
        _successAction = action;
        return this;
    }

    public ConsoleLoadingMenu OnError(Action<string> action)
    {
        _errorAction = action;
        return this;
    }

    public ConsoleLoadingMenu WithSuccessMessage(Func<string, string> messageFunc)
    {
        _successMessageFunc = messageFunc;
        return this;
    }

    public ConsoleLoadingMenu WithSuccessMessage(string message)
    {
        _defaultSuccessMessage = message;
        return this;
    }

    public ConsoleLoadingMenu WithErrorMessage(Func<string, string> messageFunc)
    {
        _errorMessageFunc = messageFunc;
        return this;
    }

    public ConsoleLoadingMenu WithErrorMessage(string message)
    {
        _defaultErrorMessage = message;
        return this;
    }

    public ConsoleLoadingMenu WithWaitForKey(bool wait)
    {
        _waitForKey = wait;
        return this;
    }

    public void Show(Func<bool> action)
    {
        Show(() => action() ? (true, string.Empty) : (false, string.Empty));
    }

    public void Show(Func<(bool success, string output)> action)
    {
        Console.Clear();
        Console.ResetColor();

        var border = ConsoleBorder
            .Create(_borderWidth)
            .AddCell(1, out var cell, 1)
            .WithTitle(_titleText);

        border.Draw(centered: true);

        bool success = false;
        string output = string.Empty;
        
        using (var spinner = new ConsoleSpinner(_loadingText, SpinnerStyle.Line))
        {
            spinner.Start(text => cell.WriteLine(text, 0, foreground: ConsoleColor.Yellow));
            
            try
            {
                (success, output) = action();
            }
            catch (Exception ex)
            {
                success = false;
                output = ex.Message;
            }
            
            spinner.Stop();
        }

        string message;

        if (success)
        {
            message = _successMessageFunc != null 
                ? _successMessageFunc(output) 
                : _defaultSuccessMessage;
        }
        else
        {
            message = _errorMessageFunc != null 
                ? _errorMessageFunc(output) 
                : (string.IsNullOrEmpty(output) ? _defaultErrorMessage : $"{_defaultErrorMessage}: {output}");
        }

        DisplayResult(cell, success, message);
    }

    private void DisplayResult(dynamic cell, bool success, string message)
    {
        cell.WriteLine(message, foreground: success ? ConsoleColor.Green : ConsoleColor.Red);
        
        if (success)
        {
            _successAction?.Invoke(message);
        }
        else
        {
            _errorAction?.Invoke(message);
        }

        if (_waitForKey)
        {
            Console.ReadKey();
        }
    }
}