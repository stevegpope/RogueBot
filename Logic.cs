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
            if (monster != null)
            {
                // Low on health
                if (player.Hp < player.HpMax / 2)
                {
                    // Get the hell out of there
                    var scroll = player.InventoryItems.FirstOrDefault(i => i.Name.Contains("scroll of teleport", StringComparison.OrdinalIgnoreCase));
                    if (scroll != null)
                    {
                        player.ReadScroll(scroll);
                        return C.Search;
                    }

                    // Heal up and continue the fight
                    var potion = player.InventoryItems.FirstOrDefault(i => i.IsPotion && i.Name.Contains("healing", StringComparison.OrdinalIgnoreCase));
                    if (potion != null)
                    {
                        player.QuaffPotion(potion);
                    }
                }

                return GetMoveTowards(player, monster.Position);
            }

            // Low on health?
            if (player.Hp < player.HpMax / 2)
            {
                // Search while we wait
                return C.Search;
            }

            if (_searchTurnsRemaining > 0)
            {
                _searchTurnsRemaining--;
                return C.Search;
            }

            // Automatically search every other turn for now to find our way out of places
            char move = Explore(player);

            try
            {
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

                if (currentChar == C.StairsDown && ReadyToMoveOn(map))
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
                        // Get out of here unless we have no choice
                        var visits = 0;
                        _visited.TryGetValue((player.Position.X, player.Position.Y), out visits);

                        if (room.MonsterSet.Count > 1 && visits < 3)
                        {
                            Debug.WriteLine("Too many monsters, run!!");
                            move = Flee(player, room.MonsterSet.First());
                        }
                        else if (room.MonsterSet.Count > 0)
                        {
                            var previousRoom = _previousMap?.Rooms.FirstOrDefault(r => r.Player != null);
                            if (previousRoom != null)
                            {
                                var previousMonster = previousRoom.MonsterSet.FirstOrDefault();
                                monster = room.MonsterSet.First();
                                if (previousMonster?.Position != monster?.Position)
                                {
                                    Debug.WriteLine("Waiting for monster to come to us");
                                    return C.Rest;
                                }
                            }
                            else
                            {
                                // Single monster, see if he comes to us (then we get first strike)
                                Debug.WriteLine("Check if monster will come to us");
                                return C.Rest;
                            }
                        }

                        move = GetMoveTowards(player, target);
                    }
                    else if (currentChar != C.Door)
                    {
                        // Go to the least visited Door if the room is complete
                        if (room.IsComplete(player))
                        {
                            Position leastVisitedDoor = null;
                            int leastVisits = int.MaxValue;
                            foreach (var door in room.Doors)
                            {
                                var pos = (door.X, door.Y);
                                int visitCount = _visited.ContainsKey(pos) ? _visited[pos] : 0;
                                if (visitCount < leastVisits)
                                {
                                    if (visitCount > 5)
                                    {
                                        // We may be stuck, skip this one to explore
                                        continue;
                                    }

                                    leastVisitedDoor = door;
                                    leastVisits = visitCount;
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
                // Mark current position visited
                var currentPos = (player.Position.X, player.Position.Y);
                if (player.Position != _previousPosition && _visited.ContainsKey(currentPos))
                {
                    _visited[currentPos]++;

                    if (_visited[currentPos] > 3 && NearWall(player))
                    {
                        // We are stuck somewhere, start searching every other turn
                        _searchTurnsRemaining++;
                    }
                }
                else
                {
                    _visited[currentPos] = 1;
                }

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

        private bool NearWall(Player player)
        {
            var map = player.Map;
            var p = player.Position;
            var wallCount = 0;
            for (int x = p.X - 1; x <= p.X + 1; x++)
            {
                for (int y = p.Y - 1; y <= p.Y + 1; y++)
                {
                    if (x < 0 || y < 0 || y >= map.Maps.Length || x >= map.Maps[y].Length)
                        continue;

                    char c = map.Maps[y][x];
                    if (c == C.WallSide || c == C.WallTop || c == C.Space)
                    {
                        wallCount++;
                    }
                }
            }

            // Wall count = 0, middle of a room
            // Wall count = 1, wall side
            // Wall count = 2, door or double-ended path
            // Wall count = 3, corner or dead end
            return wallCount == 1 || wallCount == 3;
        }

        private char Flee(Player player, Monster monster)
        {
            // Try to move in the opposite direction of the monster
            var map = player.Map;
            var playerPos = map.Player;

            var room = map.Rooms.FirstOrDefault(r => r.Player != null);
            if (room != null)
            {
                // Try to find a door that leads away from the monster
                Position bestDoor = null;
                foreach (var door in room.Doors)
                {
                    var distanceToMonster = Math.Sqrt(Math.Pow(monster.Position.X - door.X, 2) + Math.Pow(monster.Position.Y - door.Y, 2));
                    var distanceToPlayer = Math.Sqrt(Math.Pow(playerPos.X - door.X, 2) + Math.Pow(playerPos.Y - door.Y, 2));
                    if (distanceToMonster > distanceToPlayer)
                    {
                        bestDoor = door;
                        break;
                    }
                }

                if (bestDoor != null)
                {
                    return GetMoveTowards(player, bestDoor);
                }
            }

            return RunAwayFromMonster(player, monster.Position);
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
            var beenEveryWhereTwice = _visited.Count > 4 && _visited.All(v => v.Value > 1);

            return visitedRooms >= 5 || beenEveryWhereTwice;
        }

        private Monster GetCloseMonster(Player player, Map currentMap)
        {
            var maps = currentMap.Maps;

            Monster GetMonsterAt(int x, int y)
            {
                if (Monster.Start(maps, x, y))
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

            if (bestMove == C.Search)
            {
                Debug.WriteLine("serach");
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
            var next = GetNextPosition(p, move);
            return Walkable(player.Map, next);
        }

        public char GetMoveTowards(Player player, Position target)
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

        public char RunAwayFromMonster(Player player, Position monster)
        {
            var start = player.Position;

            var validMoves = new List<char>();

            var left = false;
            var right = false;
            var up = false;
            var down = false;

            // Simple greedy movement
            if (monster.X <= start.X)
            {
                right = true;
            }
            if (monster.X >= start.X)
            {
                left = true;
            }
            if (monster.Y >= start.Y)
            {
                up = true;
            }
            if (monster.Y <= start.Y)
            {
                down = true;
            }

            if (left && Walkable(player.Map, GetNextPosition(start, C.Left))) validMoves.Add(C.Left);
            if (right && Walkable(player.Map, GetNextPosition(start, C.Right))) validMoves.Add(C.Right);
            if (up && Walkable(player.Map, GetNextPosition(start, C.Up))) validMoves.Add(C.Up);
            if (down && Walkable(player.Map, GetNextPosition(start, C.Down))) validMoves.Add(C.Down);

            if (validMoves.Any())
            {
                return validMoves[Random.Shared.Next(validMoves.Count)];
            }

            // Stuck, go at him!
            return GetMoveTowards(player, monster);
        }

        public bool Walkable(Map map, Position p)
        {
            var x = p.X;
            var y = p.Y;

            if (y < 0 || y >= 23 || x < 0 || x >= map.Maps[0].Length)
                return false;

            char c = map.Maps[y][x];

            if (c == C.Trap)
            {
                // Only allowed to step on traps if we are stuck
                if (_visited.TryGetValue((map.Player.X, map.Player.Y), out var value))
                {
                    if (value < 5)
                    {
                        return false;
                    }
                }
            }

            return c != C.WallSide &&
                   c != C.WallTop &&
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