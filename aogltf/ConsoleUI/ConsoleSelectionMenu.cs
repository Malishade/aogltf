namespace ConsoleUI;

internal static class ConsoleSelectionMenu
{
    public static SelectionMenu<T> Create<T>()
    {
        return new SelectionMenu<T>();
    }

    internal class SelectionMenu<T>
    {
        private List<MenuItem<T>> _items;
        private string _title = "";
        private string _searchPrompt = "";
        private Func<T, string> _displayFunc = item => item?.ToString() ?? "";
        private Func<string, T, bool>? _filterFunc = null;
        private bool _enableSearch = false;
        private Func<T, bool>? _onSelect = null;
        private bool _loop = false;
        private bool _escapePressed = false;
        private int _borderWidth = 80;

        public class MenuItem<TValue>
        {
            public TValue Value { get; set; }
            public string Display { get; set; }

            public MenuItem(TValue value, string display)
            {
                Value = value;
                Display = display;
            }
        }

        internal SelectionMenu()
        {
            _items = new List<MenuItem<T>>();
        }

        public SelectionMenu<T> WithItems(IEnumerable<T> items)
        {
            _items = items.Select(item => new MenuItem<T>(item, _displayFunc(item))).ToList();
            return this;
        }

        public SelectionMenu<T> WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public SelectionMenu<T> WithDisplayFunc(Func<T, string> displayFunc)
        {
            _displayFunc = displayFunc;
            return this;
        }

        public SelectionMenu<T> WithFilterFunc(Func<string, T, bool> filterFunc)
        {
            _filterFunc = filterFunc;
            return this;
        }

        public SelectionMenu<T> EnableSearch(bool enable = true)
        {
            _enableSearch = enable;
            return this;
        }

        public SelectionMenu<T> WithLoop(bool loop = true)
        {
            _loop = loop;
            return this;
        }

        public SelectionMenu<T> OnSelect(Func<T, bool> onSelect)
        {
            _onSelect = onSelect;
            return this;
        }

        public SelectionMenu<T> WithBorderWidth(int width)
        {
            _borderWidth = Math.Min(width, Console.WindowWidth);
            return this;
        }
        public SelectionMenu<T> WithDynamicHeight()
        {
            _dynamicHeight = true;
            return this;
        }

        private bool _dynamicHeight = false;

        private int CalculateMaxDisplay()
        {
            int consoleHeight = Console.WindowHeight;
            int reservedLines = 2;

            if (_enableSearch)
            {
                if (!string.IsNullOrWhiteSpace(_searchPrompt))
                    reservedLines += 2;
                reservedLines += 6;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_title))
                    reservedLines += 2;
            }

            reservedLines += 2;

            int availableLines = consoleHeight - reservedLines;

            return Math.Max(5, availableLines);
        }

        public T? Show()
        {
            var loop = _loop ? ShowLoop() : ShowOnce();
            return loop;
        }

        private T? ShowLoop()
        {
            while (true)
            {
                Console.Clear();
                var selected = ShowOnce();

                if (_escapePressed)
                {
                    return default;
                }

                if (_onSelect != null)
                {
                    bool shouldContinue = _onSelect(selected);
                    if (!shouldContinue)
                    {
                        return selected;
                    }
                }
                else
                {
                    return selected;
                }
            }
        }

        private T? ShowOnce()
        {
            var maxDisplay = CalculateMaxDisplay();
            _escapePressed = false;
            _items = _items.Select(item => new MenuItem<T>(item.Value, _displayFunc(item.Value))).ToList();

            string searchTerm = "";
            int selectedIndex = 0;
            int scrollOffset = 0;
            int startLine = Console.CursorTop;
            int previousDisplayCount = 0;

            while (true)
            {
                var displayItems = _items;

                if (_enableSearch && !string.IsNullOrWhiteSpace(searchTerm) && _filterFunc != null)
                {
                    displayItems = _items.Where(item => _filterFunc(searchTerm, item.Value)).ToList();
                }

                if (selectedIndex >= displayItems.Count)
                    selectedIndex = Math.Max(0, displayItems.Count - 1);

                if (selectedIndex < scrollOffset)
                {
                    scrollOffset = selectedIndex;
                }
                else if (selectedIndex >= scrollOffset + maxDisplay)
                {
                    scrollOffset = selectedIndex - maxDisplay + 1;
                }

                Console.SetCursorPosition(0, startLine);
                int linesToClear = Math.Max(maxDisplay + 10, previousDisplayCount + 10);
                
                for (int i = 0; i < linesToClear; i++)
                {
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.WriteLine();
                }
                Console.SetCursorPosition(0, startLine);

                var border = ConsoleBorder
                    .Create(_borderWidth)
                    .WithBorderColor(ConsoleColor.DarkGray)
                    .WithTopBorderText(_title, ConsoleColor.Yellow).AddEmptyLine();

                if (_enableSearch)
                {
                    if (!string.IsNullOrWhiteSpace(_searchPrompt))
                    {
                        border.AddLine(_searchPrompt, ConsoleColor.DarkGray);
                    }

                    border.AddLine($"Search: {searchTerm}_", ConsoleColor.Yellow)
                        .AddEmptyLine()
                        .AddSeparator();
                }

                int displayCount = Math.Min(maxDisplay, displayItems.Count - scrollOffset);

                for (int i = 0; i < displayCount; i++)
                {
                    int itemIndex = scrollOffset + i;
                    var item = displayItems[itemIndex];
                    string displayText = item.Display;

                    if (displayText.Length > _borderWidth - 8)
                    {
                        displayText = string.Concat(displayText.AsSpan(0, _borderWidth - 11), "...");
                    }

                    if (itemIndex == selectedIndex)
                    {
                        border.AddSelectedLine($"> {displayText}");
                    }
                    else
                    {
                        border.AddLine($"  {displayText}");
                    }
                }

                if (_dynamicHeight)
                {
                    for (int i = displayCount; i < maxDisplay; i++)
                    {
                        border.AddLine("  ");
                    }
                }

                if (scrollOffset > 0 || displayItems.Count > scrollOffset + maxDisplay)
                {
                    string scrollInfo = "";
                    string totalFound = $"Total: {displayItems.Count} ";

                    if (scrollOffset > 0)
                        scrollInfo += $"↑ More above ({scrollOffset}) ";
                    if (displayItems.Count > scrollOffset + maxDisplay)
                        scrollInfo += $"↓ More below ({displayItems.Count - scrollOffset - maxDisplay} more)";

                    if (!string.IsNullOrEmpty(scrollInfo))
                    {
                        border.AddSeparator()
                              .AddLine(scrollInfo, totalFound, ConsoleColor.DarkGray, ConsoleColor.Green);
                    }
                }
                else
                {
                    border.AddLine("  ");
                    border.AddLine("  ");
                }

                previousDisplayCount = border
                    .AddEmptyLine()
                    .Draw(centered: true);

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    ClearDisplay(startLine, linesToClear);
                    _escapePressed = true;
                    return default;
                }
                else if (key.Key == ConsoleKey.Enter && displayItems.Count > 0)
                {
                    ClearDisplay(startLine, linesToClear);
                    return displayItems[selectedIndex].Value;
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex = selectedIndex == 0 ? displayItems.Count - 1 : selectedIndex - 1;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex = displayItems.Count > 0 ? (selectedIndex + 1) % displayItems.Count : 0;
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    selectedIndex = Math.Max(0, selectedIndex - maxDisplay);
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    selectedIndex = Math.Min(displayItems.Count - 1, selectedIndex + maxDisplay);
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    selectedIndex = 0;
                }
                else if (key.Key == ConsoleKey.End)
                {
                    selectedIndex = displayItems.Count - 1;
                }
                else if (_enableSearch && key.Key == ConsoleKey.Backspace && searchTerm.Length > 0)
                {
                    searchTerm = searchTerm.Substring(0, searchTerm.Length - 1);
                    selectedIndex = 0;
                    scrollOffset = 0;
                }
                else if (_enableSearch && !char.IsControl(key.KeyChar))
                {
                    searchTerm += key.KeyChar;
                    selectedIndex = 0;
                    scrollOffset = 0;
                }
            }
        }

        private void ClearDisplay(int startLine, int linesToClear)
        {
            Console.SetCursorPosition(0, startLine);
            for (int i = 0; i < linesToClear; i++)
            {
                Console.Write(new string(' ', Console.WindowWidth));
                Console.WriteLine();
            }
            Console.SetCursorPosition(0, startLine);
        }
    }
}