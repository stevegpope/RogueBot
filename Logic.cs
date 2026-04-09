namespace RogueBot
{
    internal class Logic
    {
        private char? previousMove = null;

        // Track explored tiles
        private readonly HashSet<(int x, int y)> visited = new();

        internal char ChooseMove(Player player, Map previousMap)
        {
            char move = C.Rest;

            try
            {
                var map = player.Map;

                // Mark current position visited
                visited.Add((player.Position.X, player.Position.Y));

                char? currentChar = null;
                if (previousMap != null && previousMove != C.DownStairs)
                {
                    currentChar = previousMap.Maps[player.Position.Y][player.Position.X];
                }

                if (map.Maps.Any(m => m.Contains("REST")))
                {
                    move = C.Enter;
                }
                else if (map.Maps.Any(m => m.Contains("More")))
                {
                    move = C.Space;
                }
                else if (currentChar == C.StairsDown)
                {
                    move = C.DownStairs;
                }
                else
                {
                    var room = map.Rooms.FirstOrDefault(r => r.Player != null);

                    if (room != null)
                    {
                        var target = room.ChooseTarget(player, currentChar);

                        if (target != null)
                        {
                            move = GetMoveTowards(player, target);
                        }
                        else
                        {
                            move = Explore(player);
                        }
                    }
                    else
                    {
                        move = Explore(player);
                    }
                }
            }
            finally
            {
                previousMove = move;
            }

            return move;
        }

        private char Explore(Player player)
        {
            var moves = new[] { C.Up, C.Down, C.Left, C.Right };

            // 1. Prefer unvisited tiles
            foreach (var move in moves)
            {
                if (CanMove(player, move))
                {
                    var next = GetNextPosition(player.Position, move);
                    if (!visited.Contains((next.X, next.Y)))
                    {
                        return move;
                    }
                }
            }

            // 2. Try continuing forward
            if (previousMove != null && CanMove(player, previousMove.Value))
            {
                return previousMove.Value;
            }

            // 3. Try any valid move
            foreach (var move in moves)
            {
                if (CanMove(player, move))
                {
                    return move;
                }
            }

            return C.Rest;
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

        internal static char GetMoveTowards(Player player, Position target)
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

            return C.Rest;
        }

        internal static bool Walkable(Player player, int x, int y)
        {
            var map = player.Map;

            if (y < 0 || y >= map.Maps.Count || x < 0 || x >= map.Maps.First().Length)
                return false;

            char c = map.Maps[y][x];

            return c != C.WallSide &&
                   c != C.WallTop &&
                   c != C.Trap &&
                   !char.IsWhiteSpace(c);
        }
    }
}