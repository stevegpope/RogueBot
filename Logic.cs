using System.Diagnostics;
using System.Numerics;

namespace RogueBot
{
    public class Logic
    {
        private char? previousMove = null;
        public Position previousPosition = null;
        private int restTurnsRemaining = 0;
        private DateTime LastRest = DateTime.MinValue;

        // Track explored tiles
        public static Dictionary<(int x, int y), int> visited = new();

        // Track recent positions (anti-oscillation)
        private Queue<(int x, int y)> lastPositions = new();
        private const int HistorySize = 50000;

        public char ChooseMove(Player player, Map previousMap)
        {
            if (restTurnsRemaining > 0)
            {
                restTurnsRemaining--;
                return C.Search;
            }

            char move = Explore(player);

            try
            {
                var map = player.Map;

                // Mark current position visited
                var currentPos = (player.Position.X, player.Position.Y);
                if (visited.ContainsKey(currentPos))
                {
                    visited[currentPos]++;
                }
                else
                {
                    visited[currentPos] = 1;
                }

                char? currentChar = null;
                if (previousMap != null && previousMove != C.DownStairs)
                {
                    currentChar = previousMap.Maps[player.Position.Y][player.Position.X];
                }

                var room = map.Rooms.FirstOrDefault(r => r.Player != null);
                if (currentChar == C.StairsDown)
                {
                    visited = new();
                    lastPositions = new();

                    move = C.DownStairs;
                }
                else if (room != null)
                {
                    var target = room.ChooseTarget(player, currentChar);
                    if (target != null)
                    {
                        move = GetMoveTowards(player, target);
                    }
                    else if (currentChar != C.Door)
                    {
                        // Go to the least visited Door if the room is complete
                        if (room.IsComplete(player))
                        {
                            Position leastVisitedDoor = null;
                            foreach (var door in room.Doors)
                            {
                                var pos = (door.X, door.Y);
                                int visitCount = visited.ContainsKey(pos) ? visited[pos] : 0;
                                if (leastVisitedDoor == null || visitCount < visited.GetValueOrDefault((leastVisitedDoor.X, leastVisitedDoor.Y), 0))
                                {
                                    leastVisitedDoor = door;
                                }
                            }

                            if (leastVisitedDoor == null && room.Doors.Count > 0)
                            {
                                leastVisitedDoor = room.Doors.First();
                            }

                            if (leastVisitedDoor != null)
                            {
                                move = GetMoveTowards(player, leastVisitedDoor);
                            }
                        }
                    }
                }
            }
            finally
            {
                previousMove = move;
                previousPosition = player.Position;

                // Track recent positions
                var pos = (player.Position.X, player.Position.Y);
                lastPositions.Enqueue(pos);

                if (lastPositions.Count > HistorySize)
                    lastPositions.Dequeue();
            }

            return move;
        }

        private char Explore(Player player)
        {
            var moves = new[] { C.Up, C.Down, C.Left, C.Right };
            var validMoves = new List<char>();

            foreach (var move in moves)
            {
                if (CanMove(player, move))
                    validMoves.Add(move);
            }

            // 🧠 DEAD END DETECTION
            if (validMoves.Count == 1 && LastRest.AddSeconds(10) < DateTime.Now)
            {
                LastRest = DateTime.Now;
                var pos = (player.Position.X, player.Position.Y);
                Debug.WriteLine("Dead end detected at " + pos);
                restTurnsRemaining = 10;
                return C.Search;
            }

            // --- existing scoring logic ---
            var bestScore = int.MaxValue;
            var bestMove = C.Search;

            foreach (var move in validMoves)
            {
                var next = GetNextPosition(player.Position, move);

                int visitCount = visited.ContainsKey((next.X, next.Y))
                    ? visited[(next.X, next.Y)]
                    : 0;

                int score = visitCount;

                if (previousPosition != null && previousPosition == next)
                {
                    score += 1000;
                }

                score += lastPositions.Contains((next.X, next.Y)) ? 5 : 0;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private static Position GetNextPosition(Position p, char move)
        {
            return move switch
            {
                C.Right => new Position(p.X + 1, p.Y),
                C.Left => new Position(p.X - 1, p.Y),
                C.Up => new Position(p.X, p.Y - 1),
                C.Down => new Position(p.X, p.Y + 1),
                _ => p
            };
        }

        private bool CanMove(Player player, char move)
        {
            var p = player.Position;

            return move switch
            {
                C.Right => Walkable(player, p.X + 1, p.Y),
                C.Left => Walkable(player, p.X - 1, p.Y),
                C.Up => Walkable(player, p.X, p.Y - 1),
                C.Down => Walkable(player, p.X, p.Y + 1),
                _ => false
            };
        }

        public static char GetMoveTowards(Player player, Position target)
        {
            var start = player.Position;

            // Simple greedy movement
            if (target.X < start.X && Walkable(player, start.X - 1, start.Y))
                return C.Left;

            if (target.X > start.X && Walkable(player, start.X + 1, start.Y))
                return C.Right;

            if (target.Y < start.Y && Walkable(player, start.X, start.Y - 1))
                return C.Up;

            if (target.Y > start.Y && Walkable(player, start.X, start.Y + 1))
                return C.Down;

            return C.Search;
        }

        public static bool Walkable(Player player, int x, int y)
        {
            var map = player.Map;

            if (y < 0 || y > 22 || x < 0 || x >= map.Maps[0].Length)
                return false;

            char c = map.Maps[y][x];

            return c != C.WallSide &&
                   c != C.WallTop &&
                   c != C.Trap &&
                   !char.IsWhiteSpace(c);
        }

    }
}