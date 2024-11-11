using System;
using System.Linq;

using Connect4Config;
using Connect4AI;

namespace Connect4Game
{
    class Game
    {
        public enum SlotState : int
        {
            EMPTY = 0,  // Playable slot; no counter is here.
            RED,        // Playable slot; red counter is here.
            YELLOW,     // Playable slot; yellow counter is here.
            BLOCK       // Unplayable slot; prevents FindAdjacentCounters from indexing out-of-bounds.
        }

        public enum Team : int
        {
            RED = 0,
            YELLOW
        }

        public enum Direction : int
        {
            VERTICAL = 0,   // Upper/Lower
            HORIZONTAL,     // Left/Right
            DIAGONAL_1,     // Lower-Left/Upper-Right
            DIAGONAL_2,     // Upper-Left/Lower-Right
            END             // Indicates the end of directions
        }

        // Grid size.
        public const int WIDTH = 7, HEIGHT = 6;
        // How long a line must be to win.
        public const int WIN_LINE_LENGTH = 4;

        // ANSI Formatting Codes.
        const string FG_RED = "\x1b[0;4;31m", FG_YELLOW = "\x1b[0;33m", FG_WHITE = "\x1b[0;37m";
        const string BG_WHITE = "\x1b[47m", BG_BLACK = "\x1b[40m";
        const string US_WHITE = "\x1b[4;37m", US_BLACK = "\x1b[4;30m";

        // What a slot looks like as empty/red/yellow/block. Block should never appear.
        static readonly string[] SLOT_CHARS = { " ", $"{FG_RED}\u25CF", $"{FG_YELLOW}\u25CF", "!" };

        // + 2 on each because we're padding the edges of the grid with BLOCK slots.
        // It's easier to check for adjacent counters that way; we don't have to worry
        // about indexing out-of-bounds.
        public static SlotState[,] grid = new SlotState[WIDTH + 2, HEIGHT + 2];

        // What X position has the user currently got selected?
        static int selectedX = 1;

        // What team is the player on?
        public static Team playerTeam = Team.YELLOW;

        // Is the player going first?
        static bool playerFirst = true;

        // Has the first move been played?
        static int playedMoves = 0;

        public static void Play()
        {
            // Clear console at the start of a new game.
            Console.Clear();

            // Reset the grid.
            ClearGrid();

            // Move selection to center slot.
            selectedX = (int)Math.Ceiling((double)(Game.WIDTH / 2)) + 1;

            // Alternate first player.
            playerFirst = !playerFirst;
            playedMoves = 0;

            while (true)
            {
                PrintGrid();

                // If we're not going first, and the first move hasn't been played yet, just pretend we gave input
                // so the AI can move.
                // If we've filled up the grid, we also do this, but instead it's so that we can tell the user they've
                // drawn.
                //
                // We have to use "intercept: true" because Windows acts weirdly if you press the escape key.
                // See https://github.com/dotnet/runtime/issues/84261 for more info.
                ConsoleKey key;
                if ((playedMoves == 0 && !playerFirst) || playedMoves == WIDTH * HEIGHT)
                    key = ConsoleKey.Spacebar;
                else
                    key = Console.ReadKey(intercept: true).Key;

                switch (key)
                {
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.B:
                        // If we're not in debug mode, ignore the debug key.
                        if (key == ConsoleKey.B && !Config.DEBUG) break;

                        int nextY = GetNextYForX(selectedX);

                        if ((playedMoves != 0 || playerFirst) && (playedMoves != WIDTH * HEIGHT))
                        {
                            if (nextY == 0) break;

                            // Set wherever the player just selected to their team, or to opponent team if they
                            // played the opponent's turn instead.
                            grid[selectedX, nextY] = playerTeam == (key != ConsoleKey.B ? Team.RED : Team.YELLOW)
                                                                   ? SlotState.RED : SlotState.YELLOW;

                            PrintGrid();

                            playedMoves++;

                            // Check to see if placing this counter created any winning lines.
                            if (CheckForLines(selectedX, nextY))
                            {
                                // Player wins!
                                Console.WriteLine("You won!");
                                return;
                            }
                        }

                        // If there are no more available slots, it's a draw.
                        if (playedMoves == WIDTH * HEIGHT)
                        {
                            Console.WriteLine("\nYou drew!");
                            return;
                        }

                        // Let the AI make a move.
                        if (!(key == ConsoleKey.B && Config.DEBUG))
                        {
                            int aiX, aiY;
                            Team aiTeam = playerTeam == Team.RED ? Team.YELLOW : Team.RED;

                            OpponentAI.PlayCounter(aiTeam, selectedX, nextY, out aiX, out aiY);

                            PrintGrid();

                            playedMoves++;

                            if (CheckForLines(aiX, aiY))
                            {
                                // Player loses!
                                Console.WriteLine("\nYou lost!");
                                return;
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        // Move the counter cursor to the left by one.
                        if (selectedX != 1) selectedX--;
                        break;
                    case ConsoleKey.RightArrow:
                        // Move the counter cursor to the right by one.
                        if (selectedX != WIDTH) selectedX++;
                        break;
                    case ConsoleKey.Escape:
                        // Back out of the game.
                        return;
                    default:
                        break;
                }
            }
        }

        // Reset the grid.
        static void ClearGrid()
        {
            for (int y = 0; y < HEIGHT + 2; y++)
            {
                for (int x = 0; x < WIDTH + 2; x++)
                {
                    grid[x, y] = (x == 0 || x == WIDTH + 1 || y == 0 || y == HEIGHT + 1) ? SlotState.BLOCK : SlotState.EMPTY;
                    if (Config.DEBUG) Console.Write($"{grid[x, y]}{(x == WIDTH + 1 ? '\n' : ' ')}");
                }
            }
        }

        // Print out the current state of the grid.
        // We also print the controls here too, because it's easier to deal with our cursor movements that way.
        static void PrintGrid()
        {
            // Make the cursor invisible so it doesn't flash.
            Console.CursorVisible = false;

            // Move it to the top.
            Console.SetCursorPosition(0, 0);

            // Iterate through the grid and print it's contents out.
            for (int y = 1; y < HEIGHT + 2; y++)
            {
                if (y == HEIGHT + 1) break;

                Console.Write("\n|");

                for (int x = 1; x < WIDTH + 1; x++)
                {
                    bool isNext = (x == selectedX && y == GetNextYForX(x));

                    if (isNext)
                        Console.Write($"{US_WHITE}{(playerTeam == Team.RED ? FG_RED : FG_YELLOW)}\u25CB{US_WHITE}{FG_WHITE}|");
                    else
                        Console.Write($"{US_WHITE}{SLOT_CHARS[(int)grid[x, y]]}{FG_WHITE}|");
                }
            }

            // Print controls
            Console.Write("\n\n");
            Console.WriteLine("SPACE : Play Counter");
            if (Config.DEBUG) Console.WriteLine("B     : Play Opponent Counter (DEBUG)");
            Console.WriteLine("LEFT  : Move Counter Left");
            Console.WriteLine("RIGHT : Move Counter Right");
            Console.WriteLine("ESC   : Quit Game");

            // Remove previous characters and put the cursor back to the start of the line.
            Console.Write(" ");
            Console.CursorLeft = 0;

            // Make the cursor visible again.
            Console.CursorVisible = true;
        }

        static void TestGrid()
        {
            if (!Config.DEBUG) return;

            PrintGrid();

            grid[3, 6] = SlotState.RED;
            grid[5, 6] = SlotState.YELLOW;
            grid[5, 5] = SlotState.RED;
            grid[6, 6] = SlotState.RED;
            grid[6, 5] = SlotState.RED;
            grid[6, 4] = SlotState.RED;
            grid[6, 3] = SlotState.RED;
            grid[6, 2] = SlotState.RED;
            grid[6, 1] = SlotState.RED;

            PrintGrid();

            Console.WriteLine(GetNextYForX(2));
            Console.WriteLine(GetNextYForX(3));
            Console.WriteLine(GetNextYForX(4));
            Console.WriteLine(GetNextYForX(5));
            Console.WriteLine(GetNextYForX(6));
        }

        public static int GetNextYForX(int x)
        {
            int y = HEIGHT + 1;

            while (y != 0)
                if (grid[x, --y] == SlotState.EMPTY) break;

            return y;
        }

        public static int[][] GetDirectionVectors(Direction dir, int x, int y)
        {
            switch (dir)
            {
                case Direction.VERTICAL:
                    return new int[][] { new int[] { x - 1, y }, new int[] { x + 1, y } };
                case Direction.HORIZONTAL:
                    return new int[][] { new int[] { x, y - 1 }, new int[] { x, y + 1 } };
                case Direction.DIAGONAL_1:
                    return new int[][] { new int[] { x - 1, y - 1 }, new int[] { x + 1, y + 1 } };
                case Direction.DIAGONAL_2:
                    return new int[][] { new int[] { x - 1, y + 1 }, new int[] { x + 1, y - 1 } };
                default:
                    throw new Exception("Unknown DIRECTION!");
            }
        }

        // Call FindAdjacentCounters on a given grid coordinate for all possible directions.
        // If it sets `counted` greater than or equal to WIN_LINE_LENGTH, returns true.
        // Otherwise, returns false.
        static bool CheckForLines(int x, int y)
        {
            Team team = grid[x, y] == SlotState.RED ? Team.RED : Team.YELLOW;

            for (int d = 0; d < (int)Direction.END; d++)
            {
                if (Config.DEBUG) Console.WriteLine($"Checking {(Direction)d}...");

                int counted = 1;
                FindAdjacentCounters((Direction)d, team, -1, ref counted, x, y);

                if (counted >= WIN_LINE_LENGTH) return true;
            }

            if (Config.DEBUG) Console.ReadKey();

            return false;
        }

        // Recursively checks for consecutive counters of the same team that are connected to a given grid coordinate.
        // `counted` is set to the length of the longest line found.
        public static void FindAdjacentCounters(Direction dir, Team team, int checkDirIndex, ref int counted, int x, int y)
        {
            if (grid[x, y] == SlotState.BLOCK) return;

            int[][] dirs = GetDirectionVectors(dir, x, y);

            int nextX, nextY;

            // If this is the initial call (i.e. checkDirIndex == -1), check both forwards and backwards for this counter.
            // Otherwise, check either forwards or backwards only.
            for (int curDirIndex = (checkDirIndex == -1 ? 0 : checkDirIndex); curDirIndex < 2; curDirIndex++)
            {
                nextX = dirs[curDirIndex][0];
                nextY = dirs[curDirIndex][1];

                // If we've found a counter of the same team, increase counted and run ourselves against that grid slot.
                // Use curDirIndex instead of checkDirIndex, so that we stick to a straight line.
                if (grid[nextX, nextY] == (team == Team.RED ? SlotState.RED : SlotState.YELLOW))
                {
                    if (Config.DEBUG) Console.WriteLine($"Found adjacent counter at [{nextX}, {nextY}].");
                        
                    counted++;
                    FindAdjacentCounters(dir, team, curDirIndex, ref counted, nextX, nextY);
                }

                // If we're not checking both forwards and backwards, bail out now.
                if (checkDirIndex != -1) return;
            }
        }
    }
}