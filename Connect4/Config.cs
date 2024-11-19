using Connect4Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect4Config
{
    // Declare a type for sanitize functions.
    public delegate void SanitizeFunc();

    class Config
    {
        /* ---------------- CONSTANTS ---------------- */

        // Is debugging mode enabled?
        public const bool DEBUG = !true;

        // Grid size limits and defaults.
        public const int MIN_WIDTH = 7, MIN_HEIGHT = 6,
                         MAX_WIDTH = 20, MAX_HEIGHT = 19;
        public const int DEF_WIDTH = 7, DEF_HEIGHT = 6;

        // Win line length limits and defaults.
        public const int MIN_WIN_LINE_LENGTH = 4, MAX_WIN_LINE_LENGTH = 10;
        public const int DEF_WIN_LINE_LENGTH = 4;

        /* ---------------- SETTINGS ---------------- */

        // Current grid size.
        public static int width = DEF_WIDTH, height = DEF_HEIGHT;

        // How long a line must currently be to win.
        public static int winLineLength = DEF_WIN_LINE_LENGTH;

        /* ---------------- METHODS ---------------- */

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
    }
}

