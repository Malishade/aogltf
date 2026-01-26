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
        private bool _showInfo = false;
        private bool _escapePressed = false;
        private int _borderWidth = 80;
        private bool _dynamicHeight = false;
        private ConsoleColor _foreground = ConsoleColor.Yellow;

        public class MenuItem<TValue>
        {
            public TValue Value;
            public string Display;

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

        public SelectionMenu<T> WithSearchPrompt(string prompt)
        {
            _searchPrompt = prompt;
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

        public SelectionMenu<T> WithShowInfo(bool showInfo = true)
        {
            _showInfo = showInfo;
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

        public SelectionMenu<T> WithDynamicHeight(bool enable = true)
        {
            _dynamicHeight = enable;
            return this;
        }

        public T? Show()
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
            _escapePressed = false;
            _items = [.. _items.Select(item => new MenuItem<T>(item.Value, _displayFunc(item.Value)))];

            var border = ConsoleBorder
                .Create(_borderWidth)
                .WithTitle(_title);

            ConsoleBorder.Cell searchCell = null;
            ConsoleBorder.Cell itemsCell = null;
            ConsoleBorder.Cell scrollInfoCell = null;

            if (_enableSearch)
            {
                border.AddCell(1, out searchCell, 1);
            }

            int itemsHeight;
            if (_dynamicHeight)
            {
                int availableHeight = Console.WindowHeight - 4;
                if (_enableSearch)
                    availableHeight -= (searchCell?.PaddedHeight ?? 0) + 4;
                availableHeight -= 2;
                itemsHeight = Math.Max(5, availableHeight);
            }
            else
            {
                itemsHeight = _items.Count;
            }

            border.AddCell(itemsHeight, out itemsCell, 1);

            if (_showInfo)
            {
                border.AddCell(1, out scrollInfoCell);
                scrollInfoCell.PaddingX += 2;
            }

            border.Draw(centered: true);

            string searchTerm = "";
            int selectedIndex = 0;
            int scrollOffset = 0;
            int previousSelectedIndex = -1;
            int previousScrollOffset = -1;
            string previousSearchTerm = "";
            int previousDisplayCount = 0;

            int maxItemsDisplay = itemsCell.Height - itemsCell.PaddingY * 2;

            if (_enableSearch)
            {
                int line = 0;
                if (!string.IsNullOrWhiteSpace(_searchPrompt))
                {
                    searchCell.WriteLine(_searchPrompt, line++, ConsoleColor.Gray);
                }
                searchCell.WriteLine($"Search: {searchTerm}_", line, foreground: _foreground);
            }

            while (true)
            {
                var displayItems = _items;

                if (_enableSearch && !string.IsNullOrWhiteSpace(searchTerm) && _filterFunc != null)
                {
                    displayItems = _items.Where(item => _filterFunc(searchTerm, item.Value)).ToList();
                }

                if (selectedIndex >= displayItems.Count && displayItems.Count > 0)
                {
                    selectedIndex = displayItems.Count - 1;
                }

                if (selectedIndex < 0 && displayItems.Count > 0)
                {
                    selectedIndex = 0;
                }

                if (selectedIndex < scrollOffset)
                {
                    scrollOffset = selectedIndex;
                }
                else if (selectedIndex >= scrollOffset + maxItemsDisplay)
                {
                    scrollOffset = selectedIndex - maxItemsDisplay + 1;
                }

                if (_enableSearch && searchTerm != previousSearchTerm)
                {
                    int line = string.IsNullOrWhiteSpace(_searchPrompt) ? 0 : 1;
                    searchCell.WriteLine($"Search: {searchTerm}_", line, foreground: _foreground);
                }

                bool itemsChanged = displayItems.Count != previousDisplayCount || scrollOffset != previousScrollOffset;

                if (itemsChanged)
                {
                    int visibleCount = Math.Min(maxItemsDisplay, displayItems.Count - scrollOffset);

                    for (int i = 0; i < visibleCount; i++)
                    {
                        int itemIndex = scrollOffset + i;
                        var item = displayItems[itemIndex];
                        DrawItem(itemsCell, i, item, itemIndex == selectedIndex);
                    }

                    for (int i = visibleCount; i < maxItemsDisplay; i++)
                    {
                        itemsCell.Clear(i);
                    }

                    if (_showInfo)
                    {
                        UpdateScrollInfo(scrollInfoCell, displayItems.Count, scrollOffset, maxItemsDisplay);
                    }
                }
                else if (selectedIndex != previousSelectedIndex)
                {
                    if (previousSelectedIndex >= scrollOffset && previousSelectedIndex < scrollOffset + maxItemsDisplay)
                    {
                        int prevLineOffset = previousSelectedIndex - scrollOffset;
                        DrawItem(itemsCell, prevLineOffset, displayItems[previousSelectedIndex], false);
                    }

                    if (selectedIndex >= scrollOffset && selectedIndex < scrollOffset + maxItemsDisplay)
                    {
                        int currLineOffset = selectedIndex - scrollOffset;
                        DrawItem(itemsCell, currLineOffset, displayItems[selectedIndex], true);
                    }
                }

                previousSelectedIndex = selectedIndex;
                previousScrollOffset = scrollOffset;
                previousSearchTerm = searchTerm;
                previousDisplayCount = displayItems.Count;

                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                {
                    _escapePressed = true;
                    return default;
                }
                else if (key.Key == ConsoleKey.Enter && displayItems.Count > 0)
                {
                    return displayItems[selectedIndex].Value;
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex = selectedIndex <= 0 ? displayItems.Count - 1 : selectedIndex - 1;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex = displayItems.Count > 0 ? (selectedIndex + 1) % displayItems.Count : 0;
                }
                else if (key.Key == ConsoleKey.PageUp)
                {
                    selectedIndex = Math.Max(0, selectedIndex - maxItemsDisplay);
                }
                else if (key.Key == ConsoleKey.PageDown)
                {
                    selectedIndex = Math.Min(displayItems.Count - 1, selectedIndex + maxItemsDisplay);
                }
                else if (key.Key == ConsoleKey.Home)
                {
                    selectedIndex = 0;
                }
                else if (key.Key == ConsoleKey.End)
                {
                    selectedIndex = Math.Max(0, displayItems.Count - 1);
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

        private void DrawItem(ConsoleBorder.Cell cell, int lineOffset, MenuItem<T> item, bool isSelected)
        {
            string displayText = item.Display;
            int maxWidth = cell.PaddedWidth - 2;

            if (displayText.Length > maxWidth)
            {
                displayText = string.Concat(displayText.AsSpan(0, maxWidth - 3), "...");
            }

            string prefix = isSelected ? "> " : "  ";
            string content = prefix + displayText;

            cell.WriteLine(
                content,
                lineOffset,
                isSelected ? ConsoleColor.Black : ConsoleColor.Gray,
                isSelected ? _foreground : cell.BackgroundColor
            );
        }

        private void UpdateScrollInfo(ConsoleBorder.Cell cell, int totalCount, int scrollOffset, int maxDisplay)
        {
            string scrollInfo = "";
            if (scrollOffset > 0)
                scrollInfo += $"↑ {scrollOffset} above ";
            if (totalCount > scrollOffset + maxDisplay)
                scrollInfo += $"↓ {totalCount - scrollOffset - maxDisplay} below";

            string totalInfo = $"Total: {totalCount}";
            cell.WriteLine(scrollInfo, totalInfo, leftForeground: ConsoleColor.DarkCyan, rightForeground: ConsoleColor.DarkCyan);
        }
    }
}