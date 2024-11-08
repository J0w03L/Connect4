using System;
using System.Runtime.InteropServices;

using Connect4Game;

namespace Connect4Config
{
    class Config
    {
        public const bool DEBUG = !true;
    }
}

namespace Connect4
{
    internal class Program
    {
        /** 
         *  These API imports are needed to enable ANSI support on Windows 10's cmd.exe.
         *  See the following for info:
         *      https://superuser.com/a/1300251
         *      https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/ac7ay120(v=vs.100)?redirectedfrom=MSDN
         *      https://learn.microsoft.com/en-us/windows/console/setconsolemode
         *      https://learn.microsoft.com/en-us/windows/console/getconsolemode
         *      https://learn.microsoft.com/en-us/windows/console/getstdhandle
         */
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(UInt32 nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, UInt32 dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(
            IntPtr hConsoleHandle,
            [MarshalAs(UnmanagedType.U4)] out UInt32 lpMode
        );

        const UInt32 STD_OUTPUT_HANDLE = unchecked ((UInt32)(-11));
        const UInt32 ENABLE_PROCESSED_INPUT = 0x1;
        const UInt32 ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x4;

        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        static void Main(string[] args)
        {
            // Ensure that we have ANSI color support.
            // Unfortunately, the only way to do this is through the Windows API.
            // We first have to get a handle to our console's standard output.
            IntPtr conHandle = GetStdHandle(STD_OUTPUT_HANDLE);

            // Get and clear Win32 error, if any.
            Int32 winError = Marshal.GetLastWin32Error();
            if (conHandle == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Could not get stdout handle! (error code {0:X})\nPress any key to exit.", winError);
                Console.ReadKey();
                return;
            }

            // Grab our current console mode.
            if (!GetConsoleMode(conHandle, out UInt32 curConsoleMode))
            {
                winError = Marshal.GetLastWin32Error();
                Console.WriteLine("Could not get console mode! (error code {0:X})\nPress any key to exit.", winError);
                Console.ReadKey();
                return;
            }

            const UInt32 requiredMode = ENABLE_PROCESSED_INPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING;

            // See if we need to set the mode or not.
            if ((curConsoleMode & requiredMode) != requiredMode)
            {
                // We do need to set it, so let's do that.
                if (!SetConsoleMode(conHandle, curConsoleMode | requiredMode))
                {
                    winError = Marshal.GetLastWin32Error();
                    Console.WriteLine("Could not set console mode! (error code: {0:X})\nPress any key to exit.", winError);
                    Console.ReadKey();
                    return;
                }
            }
            
            // Ensure that we can print Unicode characters.
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // We're ready; let's start the game!
            while (true)
            {
                Game.Play();

                while (true)
                {
                    Console.Write("\nPlay again? (Y/N): ");

                    ConsoleKey key = Console.ReadKey().Key;

                    if (key == ConsoleKey.N) return;
                    if (key == ConsoleKey.Y) break;
                }
            }
        }
    }
}
