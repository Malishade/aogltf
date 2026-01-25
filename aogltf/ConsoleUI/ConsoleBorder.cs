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

    public static BorderContainer Create(int width)
    {
        return new BorderContainer(width);
    }

    public class Cell
    {
        public int PaddingY = 0;
        public int PaddingX = 0;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int PaddedHeight => Height - PaddingY;
        public int PaddedWidth => Width - PaddingX;
        public ConsoleColor BackgroundColor;

        public void WriteLine(string text, int lineOffset = 0, ConsoleColor foreground = ConsoleColor.Gray, ConsoleColor? background = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (lineOffset >= PaddedHeight) 
                return;

            Console.SetCursorPosition(X, Y + lineOffset);
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background ?? BackgroundColor;

            if (text.Length > PaddedWidth)
                text = text.Substring(0, PaddedWidth);
            else
                text = text.PadRight(PaddedWidth);

            Console.Write(text);
            Console.ResetColor();
        }
        public void WriteLine(string leftText, string rightText, int lineOffset = 0, ConsoleColor leftForeground = ConsoleColor.Gray, ConsoleColor rightForeground = ConsoleColor.Gray, ConsoleColor? background = null)
        {
            if (lineOffset >= PaddedHeight)
                return;

            Console.SetCursorPosition(X, Y + lineOffset);
            Console.BackgroundColor = background ?? BackgroundColor;

            int totalLength = (leftText?.Length ?? 0) + (rightText?.Length ?? 0);

            if (totalLength > PaddedWidth)
            {
                int leftMaxLength = PaddedWidth - (rightText?.Length ?? 0);
                if (leftMaxLength > 0 && leftText != null)
                {
                    leftText = leftText.Substring(0, Math.Min(leftText.Length, leftMaxLength));
                }
                else
                {
                    leftText = "";
                }

                if (rightText != null && rightText.Length > PaddedWidth)
                {
                    rightText = rightText.Substring(0, PaddedWidth);
                }
            }

            int spacing = PaddedWidth - PaddingX - (leftText?.Length ?? 0) - (rightText?.Length ?? 0);

            if (!string.IsNullOrEmpty(leftText))
            {
                Console.ForegroundColor = leftForeground;
                Console.Write(leftText);
            }

            Console.Write(new string(' ', spacing));

            if (!string.IsNullOrEmpty(rightText))
            {
                Console.ForegroundColor = rightForeground;
                Console.Write(rightText);
            }

            Console.ResetColor();
        }
        public void Clear(int lineOffset)
        {
            if (lineOffset >= PaddedHeight) return;

            Console.SetCursorPosition(X, Y + lineOffset);
            Console.BackgroundColor = BackgroundColor;
            Console.Write(new string(' ', PaddedWidth));
            Console.ResetColor();
        }

        public void ClearAll()
        {
            for (int i = 0; i < PaddedHeight; i++)
            {
                Clear(i);
            }
        }
    }

    public struct IPoint
    {
        public int X;
        public int Y;
    }

    public class BorderContainer
    {
        private readonly int _width;
        private string? _title = null;
        private ConsoleColor _borderColor = ConsoleColor.DarkGray;
        private ConsoleColor _titleColor = ConsoleColor.Yellow;
        private ConsoleColor _backgroundColor = ConsoleColor.Black;
        private List<Cell> _cells = new List<Cell>();

        public BorderContainer(int width)
        {
            _width = width;
        }

        public BorderContainer WithTitle(string title, ConsoleColor color = ConsoleColor.White)
        {
            _title = title;
            _titleColor = color;
            return this;
        }

        public BorderContainer WithBorderColor(ConsoleColor color)
        {
            _borderColor = color;
            return this;
        }

        public BorderContainer WithBackgroundColor(ConsoleColor color)
        {
            _backgroundColor = color;
            return this;
        }


        public BorderContainer AddCell(int height, out Cell cell, int padding = 0)
        {
            cell = new Cell
            {
                PaddingY = padding,
                PaddingX = padding,
                Height = height,
            };

            _cells.Add(cell);

            return this;
        }

        public void Draw(bool centered = false)
        {
            int totalHeight = 2;
            totalHeight += _cells.Sum(c => c.Height);
            totalHeight += _cells.Count - 1;

            int left = 0;
            int top = 0;

            if (centered)
            {
                left = Math.Max(0, (Console.WindowWidth - _width) / 2);
                top = Math.Max(0, (Console.WindowHeight - totalHeight) / 2);
            }

            int contentWidth = _width - 2;

            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = _borderColor;

            if (!string.IsNullOrWhiteSpace(_title))
            {
                string text = _title;
                int maxTextLength = _width - 6;
                if (text.Length > maxTextLength)
                    text = text.Substring(0, maxTextLength);

                int textBlockWidth = text.Length + 2;
                int remaining = contentWidth - textBlockWidth;
                int leftLineLength = remaining / 2;
                int rightLineLength = remaining - leftLineLength;

                Console.BackgroundColor = _backgroundColor;
                Console.Write(TopLeft);
                Console.Write(new string(Horizontal, leftLineLength));
                Console.Write(' ');
                Console.ForegroundColor = _titleColor;
                Console.Write(text);
                Console.ForegroundColor = _borderColor;
                Console.Write(' ');
                Console.Write(new string(Horizontal, rightLineLength));
                Console.Write(TopRight);
            }
            else
            {
                Console.BackgroundColor = _backgroundColor;
                Console.Write(TopLeft);
                Console.Write(new string(Horizontal, contentWidth));
                Console.Write(TopRight);
            }
            Console.ResetColor();

            int currentY = top + 1;

            for (int i = 0; i < _cells.Count; i++)
            {
                var cellDef = _cells[i];

                cellDef.X = left + 1 + cellDef.PaddingX;
                cellDef.Y = currentY + cellDef.PaddingY;
                cellDef.Height = cellDef.Height + cellDef.PaddingY * 2;
                cellDef.Width = contentWidth;
                cellDef.BackgroundColor = _backgroundColor;

                for (int j = 0; j < cellDef.Height; j++)
                {
                    Console.SetCursorPosition(left, currentY + j);
                    Console.ForegroundColor = _borderColor;
                    Console.BackgroundColor = _backgroundColor;
                    Console.Write(Vertical);
                    Console.Write(new string(' ', contentWidth));
                    Console.Write(Vertical);
                    Console.ResetColor();
                }

                currentY += cellDef.Height;

                if (i < _cells.Count - 1)
                {
                    Console.SetCursorPosition(left, currentY);
                    Console.ForegroundColor = _borderColor;
                    Console.BackgroundColor = _backgroundColor;
                    Console.Write(VerticalRight);
                    Console.Write(new string(Horizontal, contentWidth));
                    Console.Write(VerticalLeft);
                    Console.ResetColor();
                    currentY++;
                }
            }

            Console.SetCursorPosition(left, currentY);
            Console.ForegroundColor = _borderColor;
            Console.BackgroundColor = _backgroundColor;
            Console.Write(BottomLeft);
            Console.Write(new string(Horizontal, contentWidth));
            Console.Write(BottomRight);
            Console.ResetColor();
        }
    }
}