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
        private ConsoleColor _foregroundPrompt = ConsoleColor.White;
        private ConsoleColor _foregroundInput = ConsoleColor.Yellow;
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

            ConsoleBorder.Create(_borderWidth)
                .WithTitle(_title, ConsoleColor.White)
                .AddCell(1, out var promptCell, 1)
                .AddCell(1, out var inputCell, 1)
                .Draw(centered: true);

            promptCell.WriteLine(_prompt, foreground: _foregroundPrompt);
            inputCell.WriteLine(_defaultValue, foreground: ConsoleColor.Green);
            
            string input = "";

            while (true)
            {
                input = ReadInputWithCursor(input, inputCell);
                input = string.IsNullOrEmpty(input) ? _defaultValue : input;
                var inputIsEmpty = string.IsNullOrEmpty(input);
                
                if (!_allowEmpty && inputIsEmpty)
                {
                    inputCell.WriteLine("Input cannot be empty.", foreground: ConsoleColor.Red);
                    Thread.Sleep(2000);
                    inputCell.Clear(0);
                    continue;
                }

                if (_validator != null)
                {
                    var (isValid, message) = _validator(input);

                    if (!isValid)
                    {
                        inputCell.WriteLine(message, foreground: ConsoleColor.Red);
                        Thread.Sleep(2000);
                        inputCell.WriteLine(input + "_", foreground: _foregroundInput);
                        continue;
                    }
                    else if (!string.IsNullOrWhiteSpace(message))
                    {
                        inputCell.WriteLine(message, foreground: ConsoleColor.Green);
                        Thread.Sleep(150);
                    }
                }

                _onInput?.Invoke(inputIsEmpty ? input : _defaultValue);
                break;
            }
        }

        private string ReadInputWithCursor(string input, ConsoleBorder.Cell inputCell)
        {
            bool firstKeyPress = true;
            var cursorTimer = new System.Timers.Timer(500);
            bool showCursor = true;

            cursorTimer.Elapsed += (s, e) =>
            {
                showCursor = !showCursor;
                RedrawInput(input, inputCell, showCursor, firstKeyPress);
            };
            cursorTimer.Start();

            if (string.IsNullOrWhiteSpace(_defaultValue))
            {
                inputCell.WriteLine("_", foreground: _foregroundInput);
            }

            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                showCursor = true;

                if (key.Key == ConsoleKey.Enter)
                {
                    cursorTimer.Stop();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        input = DeleteLastWord(input);
                    }
                    else if (input.Length > 0)
                    {
                        input = input.Substring(0, input.Length - 1);
                    }

                    if (input.Length == 0 && !string.IsNullOrWhiteSpace(_defaultValue))
                    {
                        inputCell.WriteLine(_defaultValue, foreground: ConsoleColor.Green);
                        firstKeyPress = true;
                    }
                    else
                    {
                        inputCell.WriteLine(input + "_", foreground: _foregroundInput);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    if (firstKeyPress)
                    {
                        firstKeyPress = false;
                    }

                    input += key.KeyChar;
                    inputCell.WriteLine(input + "_", foreground: _foregroundInput);
                }
            }

            inputCell.WriteLine(input, foreground: _foregroundInput);
            return input;
        }
        private string DeleteLastWord(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.TrimEnd();
            int lastSpace = input.LastIndexOf(' ');

            if (lastSpace >= 0)
            {
                return input.Substring(0, lastSpace);
            }

            return "";
        }
        private void RedrawInput(string input, ConsoleBorder.Cell inputCell, bool showCursor, bool firstKeyPress)
        {
            if (input.Length == 0 && !string.IsNullOrWhiteSpace(_defaultValue) && firstKeyPress)
            {
                return;
            }

            inputCell.WriteLine(input + (showCursor ? "_" : " "), foreground: _foregroundInput);
        }
    }
}