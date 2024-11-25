using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Connect4Config;

namespace Connect4Menu
{
    // Declare a type for menu callback functions.
    public delegate void MenuButtonCallback(BaseMenu self);
    public delegate void MenuSliderCallback(BaseMenu.MenuItem self, bool increased);

    public class BaseMenu
    {
        public delegate string MenuTextFormatCallback(MenuItem self);

        public enum MenuItemType : int
        {
            BUTTON = 0,
            SLIDER
        };

        public struct MenuItem
        {
            public string text;
            public MenuItemType type;
            public MenuButtonCallback buttonCallback;
            public MenuSliderCallback sliderCallback;
            public MenuTextFormatCallback textFormatCallback;
            //public int sliderMin, sliderMax, sliderVal;
            public bool center;
        }

        private List<MenuItem> items = new List<MenuItem>();

        private string title;
        private uint top, left, width, height;
        private bool isFullscreen;

        private uint selectedItem = 0;
        private string blankBuffer;
        private bool isOpen = false;
        private uint longestItemLength = 0;
        private uint lastWidth, lastHeight;

        // Constructor function for BaseMenu; this runs whenever a menu is created with `new BaseMenu()`.
        public BaseMenu(string title, uint x, uint y, uint w, uint h, bool fullscreen = false)
        {
            if (Config.DEBUG) Console.WriteLine("Created new BaseMenu");

            this.title = title;

            this.top = y;
            this.left = x;
            this.width = w;
            this.height = h;

            this.isFullscreen = fullscreen;

            AllocateBlankBuffer();
        }

        // Adds a new menu button.
        public void AddButtonItem(string text, bool center, MenuButtonCallback callback)
        {
            MenuItem item = new MenuItem();

            item.text = text;
            item.center = center;
            item.buttonCallback = callback;

            items.Add(item);

            if (text.Length > longestItemLength) longestItemLength = (uint)text.Length;
        }

        // Adds a new menu slider.
        public void AddSliderItem(string text, bool center, /*int min, int max, int def, */ MenuSliderCallback callback, MenuTextFormatCallback formatCallback)
        {
            MenuItem item = new MenuItem();

            item.type = MenuItemType.SLIDER;
            item.text = text;
            item.center = center;
            //item.sliderMin = min;
            //item.sliderMax = max;
            //item.sliderVal = def;
            item.sliderCallback = callback;
            item.textFormatCallback = formatCallback;

            items.Add(item);

            if (text.Length + 5 > longestItemLength) longestItemLength = (uint)text.Length + 5;
        }

        // Displays the menu and allows the user to interact with it until the menu is closed.
        public void OpenMenu()
        {
            isOpen = true;

            while (isOpen)
            {
                // If this is a full-screen menu, update our size.
                if (isFullscreen)
                {
                    // Make sure our window is actually big enough to draw a menu.
                    if (Console.WindowWidth < 84 || Console.WindowHeight < 6)
                    {
                        // It's not; wait for the user to resize the window.
                        Console.Clear();
                        Console.WriteLine("Please resize this window to at least 84 x 6.");

                        while (Console.WindowWidth < 84 || Console.WindowHeight < 6)
                        {
                            // Sleep for .25 seconds; we don't need to hog the CPU in an infinite loop.
                            Thread.Sleep(250);
                        }
                    }

                    width = (uint)Console.WindowWidth;
                    height = (uint)Console.WindowHeight;

                    AllocateBlankBuffer();
                }

                // Hide the cursor.
                Console.CursorVisible = false;

                // Fill background with blue.
                FillMenuArea(left, top, width, height, "\x1b[44m");

                // Draw the menu.
                DrawMenu();

                // Wait for user input.
                //
                // We have to use "intercept: true" because Windows acts weirdly if you press the escape key.
                // See https://github.com/dotnet/runtime/issues/84261 for more info.
                MenuItem item = items[(int)selectedItem];
                switch (GetKeyPress())
                {
                    case ConsoleKey.UpArrow:
                        // Select item above current item, or wrap around to the bottom if we can't.
                        if (selectedItem > 0)
                            selectedItem--;
                        else if (selectedItem == 0)
                            selectedItem = (uint)items.Count - 1;
                        break;

                    case ConsoleKey.DownArrow:
                        // Select item below current item, or wrap around to the top if we can't.
                        if (selectedItem < items.Count - 1)
                            selectedItem++;
                        else if (selectedItem == (uint)(items.Count - 1))
                            selectedItem = 0;
                        break;

                    case ConsoleKey.LeftArrow:
                        // If we have a slider item selected, tell it to decrease it's value.
                        if (item.type == MenuItemType.SLIDER)
                            item.sliderCallback(item, false);
                        break;

                    case ConsoleKey.RightArrow:
                        // If we have a slider item selected, tell it to increase it's value.
                        if (item.type == MenuItemType.SLIDER)
                            item.sliderCallback(item, true);
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        // Run the callback function associated with the current item.
                        if (item.buttonCallback != null)
                            item.buttonCallback(this);
                        break;

                    case ConsoleKey.Escape:
                        // Close the menu.
                        isOpen = false;
                        break;

                    default:
                        break;
                }
            }

            Console.CursorVisible = true;
        }

        public void CloseMenu()
        {
            isOpen = false;
        }

        // Draws the menu to the console.
        private void DrawMenu()
        {
            int[] cursorPos = GetCursorPos();

            uint menuCenterX = left + (width / 2);
            uint menuCenterY = top + (height / 2);

            if (title.Length != 0)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(PadText(title, (uint)Console.WindowWidth - 2));
            }

            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(PadText("[ESC]: Exit   [SPACE]: Select   [UP/DOWN]: Navigate   [LEFT/RIGHT]: Modify Setting", (uint)Console.WindowWidth - 2));

            for (int i = 0; i < items.Count; i++)
            {
                MenuItem item = items[i];

                //int itemX = item.center ? ((int)menuCenterX - (item.text.Length / 2)) : (int)left;
                int itemX = item.center ? (int)((int)menuCenterX - (longestItemLength / 2)) : (int)left;
                int itemY = item.center ? ((int)menuCenterY - (items.Count / 2) + i) : (int)top + i;

                Console.SetCursorPosition(itemX, itemY);

                if (selectedItem == i) Console.Write("\x1b[30m\x1b[47m");

                switch (item.type)
                {
                    case MenuItemType.BUTTON:
                        Console.Write(PadText(item.text, longestItemLength));
                        break;

                    case MenuItemType.SLIDER:
                        Console.CursorLeft -= 4;
                        Console.Write("[<]");
                        Console.CursorLeft++;

                        Console.Write(PadText(item.textFormatCallback(item), longestItemLength));

                        Console.CursorLeft++;
                        Console.Write("[>]");
                        break;
                }
                if (selectedItem == i) Console.Write("\x1b[0m");
            }

            RestoreCursorPos(cursorPos);
        }

        // Fills an area of the screen with a given ANSI code.
        private void FillMenuArea(uint x, uint y, uint w, uint h, string ansiCode)
        {
            int[] cursorPos = GetCursorPos();

            for (int curY = 0; curY < h; curY++)
            {
                Console.SetCursorPosition((int)x, (int)y + curY);
                Console.Write(ansiCode + blankBuffer.Substring(0, (int)w) + "\x1b[0m");
            }

            RestoreCursorPos(cursorPos);
        }

        // Adds left and right white-space for uniform item text length or centering.
        private string PadText(string text, uint size)
        {
            // If we are already at the desired size, don't bother padding.
            if (text.Length >= size) return ' ' + text + ' ';

            char[] padLeft, padRight;
            uint padLeftSize = 0, padRightSize = 0;

            // If this won't be even, add 1 to the right padding.
            if ((size - text.Length) % 2 != 0) padRightSize = 1;

            padLeftSize += (uint)(size - text.Length) / 2;
            padRightSize += (uint)(size - text.Length) / 2;

            padLeft = new char[padLeftSize + 1];
            padRight = new char[padRightSize + 1];

            for (int i = 0; i <= padLeftSize; i++) padLeft[i] = ' ';
            for (int i = 0; i <= padRightSize; i++) padRight[i] = ' ';

            return new string(padLeft) + text + new string(padRight);
        }

        // Creates and fills blankBuffer with white-space characters.
        private void AllocateBlankBuffer()
        {
            char[] blankBufferChars = new char[width];
            for (int i = 0; i < width; i++) blankBufferChars[i] = ' ';
            blankBuffer = new string(blankBufferChars);
        }

        // Returns the current position of the console cursor as an int[].
        private int[] GetCursorPos()
        {
            return new int[] { Console.CursorLeft, Console.CursorTop };
        }

        // Restores a previous position of the console cursor from an int[].
        private void RestoreCursorPos(int[] pos)
        {
            Console.SetCursorPosition(pos[0], pos[1]);
        }

        // This is basically Console.ReadKey, except it returns a dummy key if the window size is changed.
        // This allows us to resize the menu appropriately when waiting for user input.
        private ConsoleKey GetKeyPress()
        {
            while (true)
            {
                if (lastWidth != Console.WindowWidth || lastHeight != Console.WindowHeight)
                {
                    lastWidth = (uint)Console.WindowWidth;
                    lastHeight = (uint)Console.WindowHeight;
                    return ConsoleKey.NoName;
                }

                if (Console.KeyAvailable)
                    return Console.ReadKey(intercept: true).Key;
            }
        }
    }
}
