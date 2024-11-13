using System;
using System.Collections.Generic;
using System.Threading;

using Connect4Config;

namespace Connect4Menu
{
    // Declare a type for menu callback functions.
    public delegate void MenuCallback(BaseMenu self);

    public class BaseMenu
    {
        public struct MenuItem
        {
            public string text;
            public MenuCallback callback;
            public bool center;
        }

        private List<MenuItem> items = new List<MenuItem>();

        private uint top, left, width, height;
        private bool isFullscreen;

        private uint selectedItem = 0;
        private string blankBuffer;
        private bool isOpen = false;
        private uint longestItemLength = 0;

        // Constructor function for BaseMenu; this runs whenever a menu is created with `new BaseMenu()`.
        public BaseMenu(uint x, uint y, uint w, uint h, bool fullscreen = false)
        {
            if (Config.DEBUG) Console.WriteLine("Created new BaseMenu");

            top = y;
            left = x;
            width = w;
            height = h;

            isFullscreen = fullscreen;

            AllocateBlankBuffer();
        }

        // Adds a new menu item.
        public void AddItem(string text, bool center, MenuCallback callback)
        {
            MenuItem item = new MenuItem();

            item.text = text;
            item.center = center;
            item.callback = callback;

            items.Add(item);

            if (text.Length > longestItemLength) longestItemLength = (uint)text.Length;
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
                    if (Console.BufferWidth < 16 || Console.BufferHeight < 6)
                    {
                        // It's not; wait for the user to resize the window.
                        Console.Clear();
                        Console.WriteLine("Please resize this window to at least 16 x 8.");

                        while (Console.BufferWidth < 16 || Console.BufferHeight < 6)
                        {
                            // Sleep for .25 seconds; we don't need to hog the CPU in an infinite loop.
                            Thread.Sleep(250);
                        }
                    }

                    width = (uint)Console.BufferWidth;
                    height = (uint)Console.BufferHeight;

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
                switch (Console.ReadKey(intercept: true).Key)
                {
                    case ConsoleKey.UpArrow:
                        // Select item above current item.
                        if (selectedItem > 0) selectedItem--;
                        break;
                    case ConsoleKey.DownArrow:
                        // Select item below current item.
                        if (selectedItem < items.Count - 1) selectedItem++;
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        // Run the callback function associated with the current item.
                        MenuItem item = items[(int)selectedItem];
                        if (item.callback != null) item.callback(this);
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

            for (int i = 0; i < items.Count; i++)
            {
                MenuItem item = items[i];

                //int itemX = item.center ? ((int)menuCenterX - (item.text.Length / 2)) : (int)left;
                int itemX = item.center ? (int)((int)menuCenterX - (longestItemLength / 2)) : (int)left;
                int itemY = item.center ? ((int)menuCenterY - (items.Count / 2) + i) : (int)top + i;

                Console.SetCursorPosition(itemX, itemY);

                if (selectedItem == i) Console.Write("\x1b[30m\x1b[47m");
                Console.Write(PadMenuItem(item.text, longestItemLength));
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

        // Adds left and right white-space for uniform item text length.
        private string PadMenuItem(string text, uint size)
        {
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
    }
}
