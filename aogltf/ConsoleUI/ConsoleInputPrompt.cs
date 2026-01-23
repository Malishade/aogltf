using static ConsoleUI.ConsoleSelectionMenu;

namespace ConsoleUI;

internal static class ConsoleInputPrompt
{
    public static InputPrompt Create()
    {
        return new InputPrompt();
    }

    internal class InputPrompt
    {
        private string _prompt = "";
        private string _defaultValue = "";
        private Func<string, (bool isValid, string? errorMessage)>? _validator = null;
        private bool _allowEmpty = false;
        private Action<string>? _onInput = null;
        private int _borderWidth = 80;
        private string _title = "";

        public InputPrompt WithPrompt(string prompt)
        {
            _prompt = prompt;
            return this;
        }

        public InputPrompt WithDefaultValue(string defaultValue)
        {
            _defaultValue = defaultValue;
            return this;
        }

        public InputPrompt WithValidator(Func<string, (bool isValid, string? errorMessage)> validator)
        {
            _validator = validator;
            return this;
        }

        public InputPrompt AllowEmpty(bool allow = true)
        {
            _allowEmpty = allow;
            return this;
        }

        public InputPrompt OnInput(Action<string> onInput)
        {
            _onInput = onInput;
            return this;
        }

        public InputPrompt WithBorderWidth(int width)
        {
            _borderWidth = Math.Min(width, Console.WindowWidth);
            return this;
        }

        public InputPrompt WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public void Show()
        {
            Console.Clear();

            var border = ConsoleBorder
                .Create(_borderWidth)
                .WithTopBorderText(_title, ConsoleColor.White)
                .WithBorderColor(ConsoleColor.DarkGray)
                .AddEmptyLine()
                .GetCenter(out int startLeft, out int startTop)
                .AddLine(_prompt, ConsoleColor.Yellow)
                .AddLine(_defaultValue, ConsoleColor.Green)
                .AddEmptyLine()
                .Draw(centered: true);

            startTop += 2;
            startLeft += 2;

            while (true)
            {
                Console.SetCursorPosition(startLeft, startTop);

                string input = ReadInputWithCursor();

                if (string.IsNullOrWhiteSpace(input))
                {
                    if (!string.IsNullOrWhiteSpace(_defaultValue))
                    {
                        input = _defaultValue;
                    }
                    else if (!_allowEmpty)
                    {
                        Console.SetCursorPosition(startLeft, startTop);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("Input cannot be empty. Please try again.".PadRight(_borderWidth - 4));
                        Console.ResetColor();
                        Thread.Sleep(2000);
                        Console.SetCursorPosition(startLeft, startTop);
                        Console.Write("".PadRight(_borderWidth - 4));
                        continue;
                    }
                }

                if (_validator != null)
                {
                    var (isValid, message) = _validator(input);

                    if (!isValid)
                    {
                        Console.SetCursorPosition(startLeft, startTop);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write((message ?? "Invalid input. Please try again.").PadRight(_borderWidth - 4));
                        Console.ResetColor();
                        Thread.Sleep(2000);
                        Console.SetCursorPosition(startLeft, startTop);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(!string.IsNullOrEmpty(_defaultValue) ? _defaultValue.PadRight(_borderWidth - 4) : string.Empty.PadRight(_borderWidth - 4));
                        Console.ResetColor();
                        continue;
                    }
                    else if (!string.IsNullOrWhiteSpace(message))
                    {
                        Console.SetCursorPosition(startLeft, startTop);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(message.PadRight(_borderWidth - 4));
                        Console.ResetColor();
                        Thread.Sleep(150);
                    }
                }

                _onInput?.Invoke(input);
                break;
            }
        }
        private string ReadInputWithCursor()
        {
            string input = "";
            bool firstKeyPress = true;
            int startX = Console.CursorLeft;
            int startY = Console.CursorTop;

            if (string.IsNullOrWhiteSpace(_defaultValue))
            {
                Console.SetCursorPosition(startX, startY);
                Console.Write("_");
                Console.SetCursorPosition(startX, startY);
            }

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input.Substring(0, input.Length - 1);
                    }

                    if (input.Length == 0 && !string.IsNullOrWhiteSpace(_defaultValue))
                    {
                        Console.SetCursorPosition(startX, startY);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(_defaultValue.PadRight(_borderWidth - 4));
                        Console.ResetColor();
                        firstKeyPress = true;

                    }
                    else
                    {

                        Console.SetCursorPosition(startX, startY);
                        string display = input + "_";
                        Console.Write(display.PadRight(_borderWidth - 4));
                        Console.SetCursorPosition(startX + input.Length, startY);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {

                    if (firstKeyPress)
                    {
                        Console.SetCursorPosition(startX, startY);
                        Console.Write("".PadRight(_borderWidth - 4));
                        firstKeyPress = false;
                    }

                    input += key.KeyChar;

                    Console.SetCursorPosition(startX, startY);
                    string display = input + "_";
                    Console.Write(display.PadRight(_borderWidth - 4));
                    Console.SetCursorPosition(startX + input.Length, startY);
                }
            }

            Console.SetCursorPosition(startX, startY);
            Console.Write(input.PadRight(_borderWidth - 4));

            return input;
        }
    }
}