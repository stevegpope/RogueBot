using System.Diagnostics;

namespace RogueBot
{
    public class Logic
    {
        private char? _previousMove = null;
        public Position _previousPosition = null;
        private Map _previousMap;
        private Room _startRoom;

        private int _searchTurnsRemaining = 0;
        private DateTime LastRest = DateTime.MinValue;

        // Track explored tiles
        public static Dictionary<(int x, int y), int> _visited = new();

        // Track recent positions (anti-oscillation)
        private Queue<(int x, int y)> _lastPositions = new();
        private const int HistorySize = 50000;
        private static readonly char[] Moves = new[] { C.Up, C.Down, C.Left, C.Right };

        public char ChooseMove(Player player, Map currentMap)
        {
            var map = UpdateMap(player, currentMap);

            if (map.HasString("more"))
                return C.Space;

            // Are we under attack?
            Monster monster = GetCloseMonster(player, currentMap);
            if (monster != null) return GetMoveTowards(player, monster.Position);

            if (_searchTurnsRemaining > 0)
            {
                _searchTurnsRemaining--;
                return C.Search;
            }

            // Automatically search every other turn for now to find our way out of places
            char move = Explore(player);

            try
            {

                // Mark current position visited
                var currentPos = (player.Position.X, player.Position.Y);
                if (_visited.ContainsKey(currentPos))
                {
                    _visited[currentPos]++;

                    if (_visited[currentPos] > 10)
                    {
                        // We are stuck somewhere, start searching every other turn
                        _searchTurnsRemaining++;
                    }
                }
                else
                {
                    _visited[currentPos] = 1;
                }

                char? currentChar = null;
                if (_previousMap != null && _previousMove != C.DownStairs)
                {
                    currentChar = _previousMap.Maps[player.Position.Y][player.Position.X];
                }

                if (player.Hungry())
                {
                    player.Eat();
                }
                else if (map.HasString(" found ") || map.HasString("now have"))
                {
                    var foundStr = map.GetString(" found ") ?? map.GetString("now have");
                    player.Use(foundStr);
                }

                var room = map.Rooms.FirstOrDefault(r => r.Player != null);

                if (currentChar == C.StairsDown)
                {
                    _visited = new();
                    _lastPositions = new();

                    move = C.DownStairs;
                }
                else if (room != null)
                {
                    var target = ChooseRoomTarget(player, room);
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
                                int visitCount = _visited.ContainsKey(pos) ? _visited[pos] : 0;
                                if (leastVisitedDoor == null || visitCount < _visited.GetValueOrDefault((leastVisitedDoor.X, leastVisitedDoor.Y), 0))
                                {
                                    if (visitCount > 5)
                                    {
                                        // We may be stuck, skip this one to explore
                                        continue;
                                    }

                                    leastVisitedDoor = door;
                                }
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
                _previousMove = move;
                _previousPosition = player.Position;

                // Track recent positions
                var pos = (player.Position.X, player.Position.Y);
                _lastPositions.Enqueue(pos);

                if (_lastPositions.Count > HistorySize)
                    _lastPositions.Dequeue();

                _previousMap = map;

            }

            return move;
        }

        private Position ChooseRoomTarget(Player player, Room room)
        {
            // Do not chase monsters if we are low on health
            if (room.MonsterSet.Any() && player.Hp > player.HpMax/2)
            {
                // Move towards the first monster
                var item = room.MonsterSet.First();
                return item.Position;
            }
            else if (room.ItemSet.Any())
            {
                var item = room.ItemSet.First();
                return item.Position;
            }
            // Do not go down the stairs until we have explored enough
            else if (room.StairsDown != null && ReadyToMoveOn(player.Map))
            {
                // Move towards the stairs
                return room.StairsDown;
            }

            return null;
        }

        private bool ReadyToMoveOn(Map map)
        {
            // If we have visited at least 5 rooms we are good to go
            var visitedRooms = 0;
            foreach (var room in map.Rooms)
            {
                if (room.Doors.Any(d => _visited.ContainsKey((d.X, d.Y))))
                {
                    visitedRooms++;
                }
            }

            // Backup, just in case there are not enough rooms
            var beenEveryWhereTwice = _visited.All(v => v.Value > 1);

            return visitedRooms >= 5 || beenEveryWhereTwice;
        }

        private Monster GetCloseMonster(Player player, Map currentMap)
        {
            var maps = currentMap.Maps;

            Monster GetMonsterAt(int x, int y)
            {
                if (Monster.Start(currentMap, x, y))
                {
                    return new Monster(new Position(x, y), maps[y][x]);
                }

                return null;
            }

            var monster = GetMonsterAt(player.Position.X - 1, player.Position.Y - 1);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X, player.Position.Y - 1);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X + 1, player.Position.Y - 1);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X - 1, player.Position.Y);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X + 1, player.Position.Y);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X - 1, player.Position.Y + 1);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X, player.Position.Y + 1);
            if (monster != null) return monster;

            monster = GetMonsterAt(player.Position.X + 1, player.Position.Y + 1);
            return monster;
        }

        private char Explore(Player player)
        {
            var validMoves = new List<char>();

            foreach (var move in Moves)
            {
                if (CanMove(player, move))
                    validMoves.Add(move);
            }

            // 🧠 DEAD END DETECTION
            if (validMoves.Count == 1 && LastRest.AddSeconds(2) < DateTime.Now)
            {
                LastRest = DateTime.Now;
                var pos = (player.Position.X, player.Position.Y);
                Debug.WriteLine("Dead end detected at " + pos);
                _searchTurnsRemaining = 10;
                return C.Search;
            }

            // --- existing scoring logic ---
            var bestScore = int.MaxValue;
            var bestMove = C.Search;

            foreach (var move in validMoves)
            {
                var next = GetNextPosition(player.Position, move);

                int visitCount = _visited.ContainsKey((next.X, next.Y))
                    ? _visited[(next.X, next.Y)]
                    : 0;

                int score = visitCount;

                if (_previousPosition != null && _previousPosition == next)
                {
                    score += 1000;
                }

                score += _lastPositions.Contains((next.X, next.Y)) ? 5 : 0;

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

        private static bool CanMove(Player player, char move)
        {
            var p = player.Position;
            var next = GetNextPosition(p, move);
            return Walkable(player.Map, next);
        }

        public static char GetMoveTowards(Player player, Position target)
        {
            var start = player.Position;

            var validMoves = new List<char>();

            var left = false;
            var right = false;
            var up = false;
            var down = false;

            // Simple greedy movement
            if (target.X < start.X)
            {
                left = true;
            }
            if (target.X > start.X) 
            { 
                right = true; 
            }
            if (target.Y > start.Y) 
            { 
                down = true; 
            }
            if (target.Y < start.Y) 
            { 
                up = true; 
            }

            if (left) validMoves.Add(C.Left);
            if (right) validMoves.Add(C.Right);
            if (up) validMoves.Add(C.Up);
            if (down) validMoves.Add(C.Down);

            if (validMoves.Any(m => Walkable(player.Map, GetNextPosition(start, m))))
            {
                return validMoves[Random.Shared.Next(validMoves.Count)];
            }

            // Random walkable move
            return Moves[Random.Shared.Next(Moves.Length)];
        }

        public static bool Walkable(Map map, Position p)
        {
            var x = p.X;
            var y = p.Y;

            if (y < 0 || y > 22 || x < 0 || x >= map.Maps[0].Length || y == map.StatusLine)
                return false;

            char c = map.Maps[y][x];

            return c != C.WallSide &&
                   c != C.WallTop &&
                   c != C.Trap &&
                   !char.IsWhiteSpace(c);
        }

        internal char ChooseMove(Map currentMap)
        {
            // Moves with no player
            if (Died(currentMap))
            {
                return C.Enter;
            }

            throw new ArgumentException($"Unsupported state: ${currentMap}");
        }

        public static bool Died(Map map)
        {
            return map.HasString("REST") || map.HasString("Score");
        }

        private Map UpdateMap(Player player, Map currentMap)
        {
            if (_startRoom == null)
            {
                _startRoom = currentMap.Rooms.First();
            }

            player.Update(currentMap);

            return currentMap;
        }
    }
}