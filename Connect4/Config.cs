using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Connect4Menu;

namespace Connect4Config
{
    // Declare a type for sanitize functions.
    public delegate void SanitizeFunc();

    class Config
    {
        /* ---------------- CONSTANTS ---------------- */

        // Is debugging mode enabled?
        public const bool DEBUG = false;

        // Grid size limits and defaults.
        public const int MIN_WIDTH = 7, MIN_HEIGHT = 6,
                         MAX_WIDTH = 20, MAX_HEIGHT = 19;
        public const int DEF_WIDTH = 7, DEF_HEIGHT = 6;

        // Win line length limits and defaults.
        public const int MIN_WIN_LINE_LENGTH = 4, MAX_WIN_LINE_LENGTH = 10;
        public const int DEF_WIN_LINE_LENGTH = 4;

        // Default setting for useOpponentAI.
        public const bool DEF_USE_OPPONENT_AI = false;

        /* ---------------- SETTINGS ---------------- */

        // Current grid size.
        public static int width = DEF_WIDTH, height = DEF_HEIGHT;

        // How long a line must currently be to win.
        public static int winLineLength = DEF_WIN_LINE_LENGTH;

        // Is the second player AI?
        public static bool useOpponentAI = DEF_USE_OPPONENT_AI;

        /* ----------------  MENUS  ---------------- */

        public static BaseMenu configMenu;

        /* ---------------- METHODS ---------------- */

        // Setup configMenu.
        public static void SetupConfigMenu()
        {
            configMenu = new BaseMenu("Settings", 0, 0, 0, 0, true);

            configMenu.AddSliderItem(
                "Grid Size: {0} x {1}",
                true,
                (item, increased) => { ClampAndWrap(increased, ref width, MIN_WIDTH, MAX_WIDTH);
                                       ClampAndWrap(increased, ref height, MIN_HEIGHT, MAX_HEIGHT);
                                       if (winLineLength > width) winLineLength = (width <= MAX_WIN_LINE_LENGTH) ? width : (width < MIN_WIN_LINE_LENGTH) ? width : MIN_WIN_LINE_LENGTH; },
                (item) => { return string.Format(item.text, width, height); }
            );
            configMenu.AddSliderItem(
                "Winning Line Length: {0}",
                true,
                (item, increased) => { ClampAndWrap(increased, ref winLineLength, MIN_WIN_LINE_LENGTH, width > MAX_WIN_LINE_LENGTH ? MAX_WIN_LINE_LENGTH : width); },
                (item) => { return string.Format(item.text, winLineLength); }
            );
            configMenu.AddSliderItem(
                "AI Opponent?: {0}",
                true,
                (item, increased) => { useOpponentAI = !useOpponentAI; },
                (item) => { return string.Format(item.text, useOpponentAI ? "Yes" : "No"); }
            );
            configMenu.AddButtonItem(
                "Revert to Defaults",
                true,
                (_) => { width = DEF_WIDTH; height = DEF_HEIGHT; winLineLength = DEF_WIN_LINE_LENGTH; useOpponentAI = DEF_USE_OPPONENT_AI; }
            );
            configMenu.AddButtonItem(
                "Back",
                true,
                (menu) => { menu.CloseMenu(); }
            );
        }

        // Ensure all current config values are sane.
        public static void SanitizeAll()
        {
            if (DEBUG) Console.WriteLine("Santizing all config vars...");

            bool wasSane = true;

            // Range checks.
            LatchFalseBool(ref wasSane, EnsureInRange(ref width, MIN_WIDTH, MAX_WIDTH, DEF_WIDTH));
            LatchFalseBool(ref wasSane, EnsureInRange(ref height, MIN_HEIGHT, MAX_HEIGHT, DEF_HEIGHT));
            LatchFalseBool(ref wasSane, EnsureInRange(ref winLineLength, MIN_WIN_LINE_LENGTH, MAX_WIN_LINE_LENGTH, DEF_WIN_LINE_LENGTH));

            // Var-specific checks.
            LatchFalseBool(ref wasSane, EnsureIsEven(width * height, () => { width = DEF_WIDTH; height = DEF_HEIGHT; }));

            if (DEBUG && !wasSane) Console.WriteLine("Found insane config vars; sanitized!");
        }

        // Test if a given config value is within a given inclusive range, restore to sane default if not.
        // Returns true or false based on if it was or not.
        public static bool EnsureInRange(ref int val, int saneMin, int saneMax, int saneDefault)
        {
            if (!(val >= saneMin && val <= saneMax))
            {
                val = saneDefault;
                return false;
            }

            return true;
        }

        // Test if a given value is even or not and return true/false according to that.
        // Calls a given callback function if false.
        public static bool EnsureIsEven(int val, SanitizeFunc callback)
        {
            bool isSane = val % 2 == 0;

            if (!isSane)
                callback();

            return isSane;
        }

        // If var is true, set var to val.
        // Otherwise, leave it as-is, so that this function can set var to false, but cannot set it back to true.
        public static void LatchFalseBool(ref bool var, bool val)
        {
            if (var) var = val;
        }

        // Common function for slider menu items.
        public static void ClampAndWrap(bool increased, ref int var, int min, int max)
        {
            if (increased)
                var = var < max ? var + 1 : min;
            else
                var = var > min ? var - 1 : max;
        }
    }
}

