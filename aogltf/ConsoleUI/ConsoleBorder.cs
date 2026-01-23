namespace ConsoleUI;

internal static class ConsoleBorder
{
    public const char TopLeft = '┌';
    public const char TopRight = '┐';
    public const char BottomLeft = '└';
    public const char BottomRight = '┘';
    public const char Horizontal = '─';
    public const char Vertical = '│';
    public const char VerticalRight = '├';
    public const char VerticalLeft = '┤';


    public static BorderBuilder Create(int width)
    {
        return new BorderBuilder(width);
    }

    public class BorderBuilder
    {
        private readonly int _width;
        private readonly List<Action> _drawActions = new();
        private string? _topBorderText = null;
        private ConsoleColor? _topBorderTextColor = null;
        private ConsoleColor? _borderBackgroundColor = null;
        private ConsoleColor? _contentBackgroundColor = null;
        private ConsoleColor? _borderForegroundColor = null;

        public BorderBuilder(int width)
        {
            _width = width;
        }

        public BorderBuilder WithTopBorderText(string text, ConsoleColor? color = null)
        {
            _topBorderText = text;
            _topBorderTextColor = color;
            return this;
        }

        public BorderBuilder WithBorderBackground(ConsoleColor color)
        {
            _borderBackgroundColor = color;
            return this;
        }

        public BorderBuilder WithBorderColor(ConsoleColor color)
        {
            _borderForegroundColor = color;
            return this;
        }

        public BorderBuilder WithContentBackground(ConsoleColor color)
        {
            _contentBackgroundColor = color;
            return this;
        }

        public BorderBuilder GetCenter(out int startLeft, out int startTop)
        {
            int windowWidth = Console.WindowWidth;
            startLeft = Math.Max(0, (windowWidth - _width) / 2);

            int windowHeight = Console.WindowHeight;
            int totalLines = 2 + _drawActions.Count;
            startTop = Math.Max(0, (windowHeight - totalLines) / 2);
            return this;
        }

        public BorderBuilder AddLine(
            string leftText,
            string rightText,
            ConsoleColor leftColor,
            ConsoleColor rightColor)
        {
            _drawActions.Add(() =>
            {
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();

                if (_contentBackgroundColor.HasValue)
                    Console.BackgroundColor = _contentBackgroundColor.Value;
                else if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                Console.Write(" ");

                int contentWidth = _width - 4;

                if (leftText.Length + rightText.Length > contentWidth)
                {
                    int maxLeft = contentWidth - rightText.Length;
                    if (maxLeft < 0) maxLeft = 0;
                    leftText = leftText.Substring(0, Math.Min(leftText.Length, maxLeft));
                }

                int spaceBetween =
                    contentWidth - leftText.Length - rightText.Length;

                Console.ForegroundColor = leftColor;
                Console.Write(leftText);

                Console.ForegroundColor = ConsoleColor.Gray;
                if (spaceBetween > 0)
                    Console.Write(new string(' ', spaceBetween));

                Console.ForegroundColor = rightColor;
                Console.Write(rightText);

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" ");

                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                else
                    Console.ResetColor();
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();
                Console.WriteLine();
            });

            return this;
        }

        public BorderBuilder AddLine(string? text, ConsoleColor? color = null)
        {
            _drawActions.Add(() =>
            {
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();

                if (_contentBackgroundColor.HasValue)
                    Console.BackgroundColor = _contentBackgroundColor.Value;
                else if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (color.HasValue)
                    Console.ForegroundColor = color.Value;
                Console.Write(" " + (text ?? string.Empty).PadRight(_width - 4) + " ");

                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                else
                    Console.ResetColor();
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();
                Console.WriteLine();
            });
            return this;
        }

        public BorderBuilder AddSelectedLine(string text)
        {
            _drawActions.Add(() =>
            {
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();

                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write(" ");
                Console.Write(text.PadRight(_width - 4));
                Console.Write(" ");

                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                else
                    Console.ResetColor();
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();
                Console.WriteLine();
            });
            return this;
        }

        public BorderBuilder AddEmptyLine()
        {
            _drawActions.Add(() =>
            {
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();

                if (_contentBackgroundColor.HasValue)
                    Console.BackgroundColor = _contentBackgroundColor.Value;
                else if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                Console.Write(new string(' ', _width - 2));

                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                else
                    Console.ResetColor();
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(Vertical);
                Console.ResetColor();
                Console.WriteLine();
            });
            return this;
        }

        public BorderBuilder AddSeparator()
        {
            _drawActions.Add(() =>
            {
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(VerticalRight);
                Console.Write(new string(Horizontal, _width - 2));
                Console.Write(VerticalLeft);
                Console.ResetColor();
                Console.WriteLine();
            });
            return this;
        }

        public int Draw(bool centered = false)
        {
            int startLeft = 0;
            int startTop = Console.CursorTop;

            if (centered)
            {
                GetCenter(out startLeft, out startTop);
            }

            if (!string.IsNullOrWhiteSpace(_topBorderText))
            {
                string text = _topBorderText;
                int maxTextLength = _width - 6;
                if (text.Length > maxTextLength)
                    text = text.Substring(0, maxTextLength);

                int contentWidth = _width - 2;
                int textBlockWidth = text.Length + 2;
                int remaining = contentWidth - textBlockWidth;

                int leftLineLength = remaining / 2;
                int rightLineLength = remaining - leftLineLength;

                Console.SetCursorPosition(startLeft, startTop);
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(TopLeft);
                Console.Write(new string(Horizontal, leftLineLength));
                Console.Write(' ');

                if (_topBorderTextColor.HasValue)
                    Console.ForegroundColor = _topBorderTextColor.Value;
                else if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(text);
                Console.ResetColor();
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;

                Console.Write(' ');
                Console.Write(new string(Horizontal, rightLineLength));
                Console.Write(TopRight);
                Console.ResetColor();
                Console.WriteLine();
            }
            else
            {
                Console.SetCursorPosition(startLeft, startTop);
                if (_borderBackgroundColor.HasValue)
                    Console.BackgroundColor = _borderBackgroundColor.Value;
                if (_borderForegroundColor.HasValue)
                    Console.ForegroundColor = _borderForegroundColor.Value;
                Console.Write(TopLeft);
                Console.Write(new string(Horizontal, _width - 2));
                Console.Write(TopRight);
                Console.ResetColor();
                Console.WriteLine();
            }

            int linesDrawn = 1;

            foreach (var action in _drawActions)
            {
                Console.SetCursorPosition(startLeft, startTop + linesDrawn);
                action();
                linesDrawn++;
            }

            Console.SetCursorPosition(startLeft, startTop + linesDrawn);
            if (_borderBackgroundColor.HasValue)
                Console.BackgroundColor = _borderBackgroundColor.Value;
            if (_borderForegroundColor.HasValue)
                Console.ForegroundColor = _borderForegroundColor.Value;
            Console.Write(BottomLeft);
            Console.Write(new string(Horizontal, _width - 2));
            Console.Write(BottomRight);
            Console.ResetColor();
            Console.WriteLine();
            linesDrawn++;

            return linesDrawn;
        }
    }
}