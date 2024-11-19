using System;

using Connect4Config;
using Connect4Game;

namespace Connect4AI
{
    class OpponentAI
    {
        public static void PlayCounter(Game.Team team, int playerX, int playerY, out int x, out int y)
        {
            // TODO: difficulty settings?
            FindSimpleMove(team, playerX, playerY, out x, out y);
            Game.grid[x, y] = team == Game.Team.RED ? Game.SlotState.RED : Game.SlotState.YELLOW;
        }

        // Find a move to play using a basic algorithm.
        // This is nothing special, came up with it in a few hours.
        // It's pretty easy to beat if you're paying attention, though.
        //
        // This "AI" will block you if placing a counter next to where you
        // last placed one would prevent you from winning.
        //
        // Otherwise, it will simply place a counter wherever would immediately
        // increase the length of it's longest unblocked line.
        //
        // It will make no attempt to judge what consequences could arise from
        // playing a move.
        static void FindSimpleMove(Game.Team team, int playerX, int playerY, out int x, out int y)
        {
            int[] bestMove = new int[3] { -1, -1, -1 };
            int centerX = (int)Math.Ceiling((double)(Config.width / 2)) + 1;

            for (int ixPair = 0; ixPair < centerX; ixPair++)
            {
                int countedHighest = 0;
                for (int pairSide = 0; pairSide <= 1; pairSide++)
                {
                    int ix = centerX - (pairSide == 0 ? ixPair : ixPair * -1);
                    int iy = Game.GetNextYForX(ix);

                    if (iy == 0) continue;

                    for (int d = 0; d < (int)Game.Direction.END; d++)
                    {
                        int counted = 1;
                        Game.FindAdjacentCounters((Game.Direction)d, team, -1, ref counted, ix, iy);
                        if (counted > countedHighest) countedHighest = counted;
                    }

                    if (countedHighest > bestMove[2])
                    {
                        bestMove[0] = ix;
                        bestMove[1] = iy;
                        bestMove[2] = countedHighest;

                        if (Config.DEBUG) Console.WriteLine($"Found new best move (simple): [{ix}, {iy}]: {countedHighest}");
                    }
                }
            }

            // If playing our best move won't win, check if we should block the player instead.
            if (bestMove[2] < Config.winLineLength)
            {
                // Check to see if the player is about to win.
                Game.Team playerTeam = team == Game.Team.RED ? Game.Team.YELLOW : Game.Team.RED;

                for (int d = 0; d < (int)Game.Direction.END; d++)
                {
                    int[][] playerDirs = Game.GetDirectionVectors((Game.Direction)d, playerX, playerY);

                    int nextPlayerX, nextPlayerY;

                    for (int curDirIndex = 0; curDirIndex <= 1; curDirIndex++)
                    {
                        nextPlayerX = playerDirs[curDirIndex][0];
                        nextPlayerY = playerDirs[curDirIndex][1];

                        // Make sure that this slot is playable
                        if (Game.grid[nextPlayerX, nextPlayerY] != Game.SlotState.EMPTY) continue;
                        if (Game.GetNextYForX(nextPlayerX) != nextPlayerY) continue;

                        int playerCounted = 1;

                        Game.FindAdjacentCounters((Game.Direction)d, playerTeam, -1, ref playerCounted, nextPlayerX, nextPlayerY);

                        if (playerCounted >= Config.winLineLength)
                        {
                            // Player is guaranteed to win if we don't move here.
                            x = nextPlayerX;
                            y = nextPlayerY;

                            if (Config.DEBUG)
                            {
                                Console.WriteLine($"Playing to block player win (simple): [{x}, {y}]");
                                Console.ReadKey();
                            }

                            return;
                        }
                    }
                }
            }

            x = bestMove[0];
            y = bestMove[1];

            if (Config.DEBUG)
            {
                Console.WriteLine($"Playing best move (simple): [{x}, {y}]: {bestMove[2]}");
                Console.ReadKey();
            }
        }
    }
}
