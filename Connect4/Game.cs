using System;
using System.Linq;

using Connect4Config;
using Connect4AI;
using Connect4Menu;
using System.Activities.Statements;
using static System.Net.Mime.MediaTypeNames;

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

        public enum Direction : int
        {
            VERTICAL = 0,   // Upper/Lower
            HORIZONTAL,     // Left/Right
            DIAGONAL_1,     // Lower-Left/Upper-Right
            DIAGONAL_2,     // Upper-Left/Lower-Right
            END             // Indicates the end of directions
        }

        public enum Team : int
        {
            RED = 0,
            YELLOW
        }

        // ANSI Formatting Codes.
        const string FG_RED = "\x1b[0;4;31m", FG_YELLOW = "\x1b[0;4;33m", FG_WHITE = "\x1b[0;37m";
        const string BG_WHITE = "\x1b[47m", BG_BLACK = "\x1b[40m";
        const string US_WHITE = "\x1b[4;37m", US_BLACK = "\x1b[4;30m";

        // What a slot looks like as empty/red/yellow/block. Block should never appear.
        static readonly string[] SLOT_CHARS = { " ", $"{FG_RED}\u25CF", $"{FG_YELLOW}\u25CF", "!" };

        // + 2 on each because we're padding the edges of the grid with BLOCK slots.
        // It's easier to check for adjacent counters that way; we don't have to worry
        // about indexing out-of-bounds.
        public static SlotState[,] grid;

        // What X position has the user currently got selected?
        static int selectedX = 1;

        // What Y position did the user last place a counter at?
        static int lastPlayerY = -1;

        // What team is the first player on?
        public static Team playerTeam = Team.RED;

        // What team is the second player on?
        public static Team otherPlayerTeam = playerTeam == Team.RED ? Team.YELLOW : Team.RED;

        // Is the player going first?
        static bool playerFirst = true;

        // Has the first move been played?
        static int playedMoves = 0;

        // Which team is currently playing?
        static bool isFirstPlayerNext = true;

        // Play a game of Connect 4!
        public static void Play()
        {
            // Clear console at the start of a new game.
            Console.Clear();

            // Create a new grid for this game.
            CreateNewGrid();

            // Move selection to center slot.
            selectedX = (int)Math.Ceiling((double)(Config.width / 2)) + 1;

            // Alternate first player.
            playerFirst = !playerFirst;
            isFirstPlayerNext = playerFirst;
            playedMoves = 0;

            // Keep looping until all possible turns have been played, the player quits, or somebody wins.
            while (true)
            {
                PrintGrid();

                // If the AI is enabled and it's the AI's turn, let the AI play.
                // If the AI is enabled but it's not the AI's turn, let the player play.
                // If the AI is disabled, we have two players, so let whichever player is next play.
                int turn = PlayNextTurn(Config.useOpponentAI, Config.useOpponentAI ? playerTeam : isFirstPlayerNext ? playerTeam : otherPlayerTeam);

                // Decide whether or not we should keep going based on the result of the turn we just played.
                switch (turn)
                {
                    case 0:
                    case -1:
                        // Game has been won/lost/drawn/quit.
                        // Game has been quit.
                        return;

                    case 1:
                        // A move has been played but did not win, or the player has yet to choose a move (moving the cursor).
                        break;

                    default:
                        break;
                }
            }
        }

        // Allocate memory for the grid based on current width/height settings.
        public static void CreateNewGrid()
        {
            grid = new SlotState[Config.width + 2, Config.height + 2];
            ClearGrid();
        }

        // Reset the grid.
        static void ClearGrid()
        {
            for (int y = 0; y < Config.height + 2; y++)
            {
                for (int x = 0; x < Config.width + 2; x++)
                {
                    grid[x, y] = (x == 0 || x == Config.width + 1 || y == 0 || y == Config.height + 1) ? SlotState.BLOCK : SlotState.EMPTY;
                    if (Config.DEBUG) Console.Write($"{grid[x, y]}{(x == Config.width + 1 ? '\n' : ' ')}");
                }
            }
            if (Config.DEBUG)
            {
                Console.ReadKey();
                Console.Clear();
            }
        }

        // Print out the current state of the grid.
        // We also print the controls here too, because it's easier to deal with our cursor movements that way.
        static void PrintGrid()
        {
            // Make the cursor invisible so it doesn't flash.
            Console.CursorVisible = false;

            // Clear the console if we're in debug mode so we don't have debug output everywhere.
            if (Config.DEBUG) Console.Clear();

            // Move it to the top.
            Console.SetCursorPosition(0, 0);

            // Iterate through the grid and print it's contents out.
            // Unlike most of the code in this file, here y starts at 0.
            // This is so that we can easily print the cursor on a full column.
            for (int y = 0; y < Config.height + 1; y++)
            {
                // If y isn't 0, start a new line and print a "\".
                // Otherwise, just print an invisible character.
                Console.Write(y != 0 ? "\n|" : " ");

                for (int x = 1; x < Config.width + 1; x++)
                {
                    int nextY = GetNextYForX(x);
                    bool isNext = (x == selectedX && y == nextY);
                    Team currentPlayerTeam = isFirstPlayerNext ? playerTeam : playerTeam == Team.RED ? Team.YELLOW : Team.RED;

                    if (isNext)
                    {
                        Console.Write($"{US_WHITE}{(currentPlayerTeam == Team.RED ? FG_RED : FG_YELLOW)}\u25CB{US_WHITE}{FG_WHITE}");
                        Console.Write(nextY != 0 ? "|" : " ");
                        continue;
                    }

                    if (y == 0)
                    {
                        Console.Write("  ");
                        continue;
                    }

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

        // Play an AI turn, or let a player play their turn.
        // Returns -1 for a game quit, 0 for a game over, and 1 otherwise.
        public static int PlayNextTurn(bool isPlayer, Team turnTeam)
        {
            // If there are no more available slots, it's a draw.
            if (playedMoves == Config.width * Config.height)
            {
                Console.WriteLine("\nYou drew!");
                return 0;
            }

            // If the AI is enabled, and it's not the player's turn, let the AI move.
            if (Config.useOpponentAI && !isFirstPlayerNext)
            {
                int aiX, aiY;
                Team aiTeam = playerTeam == Team.RED ? Team.YELLOW : Team.RED;

                OpponentAI.PlayCounter(aiTeam, selectedX, lastPlayerY, out aiX, out aiY);

                playedMoves++;
                isFirstPlayerNext = !isFirstPlayerNext;

                if (CheckForLines(aiX, aiY))
                {
                    // Player loses!
                    Console.WriteLine("\nYou lost!");
                    return 0;
                }

                return 1;
            }

            // We have to use "intercept: true" because Windows acts weirdly if you press the escape key.
            // See https://github.com/dotnet/runtime/issues/84261 for more info.
            ConsoleKey key = Console.ReadKey(intercept: true).Key;

            switch (key)
            {
                case ConsoleKey.Spacebar:
                case ConsoleKey.B:
                    // If we're not in debug mode, ignore the debug key.
                    if (key == ConsoleKey.B && !Config.DEBUG) break;

                    // Find out where the next playable Y position is on this column.
                    int nextY = GetNextYForX(selectedX);

                    // If this column is full, do nothing!
                    if (nextY == 0) break;

                    // Set wherever the player just selected to their team, or to opponent team if they
                    // played the opponent's turn instead.
                    grid[selectedX, nextY] = turnTeam == (key != ConsoleKey.B ? Team.RED : Team.YELLOW)
                                                            ? SlotState.RED : SlotState.YELLOW;

                    // For the AI, remember where the player just placed their counter. Increase move counter.
                    lastPlayerY = nextY;
                    playedMoves++;

                    // Check to see if placing this counter created any winning lines.
                    if (CheckForLines(selectedX, nextY))
                    {
                        // Player wins!
                        if (Config.useOpponentAI)
                            Console.WriteLine("\nYou won!");
                        else
                            Console.WriteLine($"\n{turnTeam} won!");
                        return 0;
                    }

                    isFirstPlayerNext = !isFirstPlayerNext;

                    break;

                case ConsoleKey.LeftArrow:
                    // Move the counter cursor to the left by one.
                    if (selectedX != 1) selectedX--;
                    break;

                case ConsoleKey.RightArrow:
                    // Move the counter cursor to the right by one.
                    if (selectedX != Config.width) selectedX++;
                    break;

                case ConsoleKey.Escape:
                    // Back out of the game.
                    return -1;

                default:
                    break;
            }

            return 1;
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

        public static int GetNextYForX(int x)
        {
            int y = Config.height + 1;

            while (y != 0)
                if (grid[x, --y] == SlotState.EMPTY) break;

            return y;
        }

        // Call FindAdjacentCounters on a given grid coordinate for all possible directions.
        // If it sets `counted` greater than or equal to WIN_LINE_LENGTH, returns true.
        // Otherwise, returns false.
        public static bool CheckForLines(int x, int y)
        {
            Team team = grid[x, y] == SlotState.RED ? Team.RED : Team.YELLOW;

            for (int d = 0; d < (int)Direction.END; d++)
            {
                if (Config.DEBUG) Console.WriteLine($"Checking {(Direction)d}...");

                int counted = 1;
                FindAdjacentCounters((Direction)d, team, -1, ref counted, x, y);

                if (counted >= Config.winLineLength) return true;
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