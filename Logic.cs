using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace RogueBot
{
    public class Logic
    {
        private char? _previousMove = null;
        public Position _previousPosition = null;
        private Map _previousMap = null;
        private Position _lastDeadEnd = null;
        private int _searchTurnsRemaining = 0;

        // Track explored tiles
        public Dictionary<(int x, int y), int> _visited = new();

        // Track recent positions (anti-oscillation)
        private Queue<(int x, int y)> _lastPositions = new();
        private bool _combat;
        private Monster _targetMonster;
        private int _turnsLeftToWaitForFight;
        private bool _doneWaitingForMonsters;
        private char _currentChar;
        private bool _startWaitingForMonsters;
        private const int HistorySize = 50000;
        private static readonly char[] Moves = new[] { C.Up, C.Down, C.Left, C.Right };

        public char ChooseMove(Player player, Map currentMap)
        {
            var map = UpdateMap(player, currentMap);

            if (map.HasString("more"))
                return C.Space;

            _currentChar = C.Unknown;
            if (_previousMap != null && _previousMove != C.DownStairs)
            {
                _currentChar = _previousMap.Maps[player.Position.Y][player.Position.X];
            }

            // Automatically search every other turn for now to find our way out of places
            char move = Explore(player);

            try
            {
                // In combat?
                if (_combat)
                {
                    move = ChooseCombatMove(player, map);
                    return move;
                }

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
                            move = C.Search;
                            return move;
                        }

                        // Heal up and continue the fight
                        var potion = player.InventoryItems.FirstOrDefault(i => i.IsPotion && i.Name.Contains("healing", StringComparison.OrdinalIgnoreCase));
                        if (potion != null)
                        {
                            player.QuaffPotion(potion);
                        }
                    }

                    move = GetMoveTowards(player, monster.Position);
                    return move;
                }

                // Low on health?
                if (player.Hp < player.HpMax * 0.75)
                {
                    // Search while we wait
                    move = C.Search;
                    return move;
                }

                if (_searchTurnsRemaining > 0)
                {
                    var validMoves = 0;
                    foreach (var possibleMove in Moves)
                    {
                        if (CanMove(player, possibleMove))
                            validMoves++;
                    }

                    // If we can move again do not search
                    if (validMoves > 1)
                    {
                        _searchTurnsRemaining = 0;
                    }
                    else
                    {
                        _searchTurnsRemaining--;
                        move = C.Search;
                        return move;
                    }
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

                if (_currentChar == C.StairsDown && ReadyToMoveOn(map))
                {
                    _visited = new();
                    _lastPositions = new();

                    Debug.WriteLine($"Down to {player.Level + 1}");
                    move = C.DownStairs;
                }
                else if (room != null)
                {
                    var target = ChooseRoomTarget(player, room);
                    if (target != null)
                    {
                        var visits = 0;
                        _visited.TryGetValue((player.Position.X, player.Position.Y), out visits);

                        if (room.MonsterSet.Count > 0)
                        {
                            StartCombat(room.MonsterSet.First());
                            return ChooseCombatMove(player, map);
                        }

                        move = GetMoveTowards(player, target);
                    }
                    else if (_currentChar != C.Door)
                    {
                        // Go to the least visited Door if the room is complete
                        if (room.IsComplete(currentMap.Maps))
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

        private void StartCombat(Monster monster)
        {
            _combat = true;
            _targetMonster = monster;
            _doneWaitingForMonsters = false;
            _turnsLeftToWaitForFight = 0;
            _startWaitingForMonsters = true;
        }

        private char ChooseCombatMove(Player player, Map map)
        {
            // Last known position will have to do
            var finishMessages = new[] { "defeated", "level" };
            if (finishMessages.Any(m => map.Details.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                EndCombat();

                // One more move in that direction to get us into the door again
                // to continue exploring
                return _previousMove.Value;
            }

            // If there is a monster next to us fight no matter what
            var monster = GetCloseMonster(player, map);
            if (monster != null)
            {
                Debug.WriteLine("Fight!");
                return GetMoveTowards(player, monster.Position);
            }

            monster = GetTargetMonster(player);

            // Are we in a good position? A path prevents us from getting attacked from more than one monster.
            var inFightPosition = Near(player, C.Path) && _currentChar != C.Door; 

            if (inFightPosition)
            {
                if (_startWaitingForMonsters)
                {
                    // Give them a little time to come to us
                    _turnsLeftToWaitForFight = (int)Math.Ceiling(CalculateDistance(player, monster.Position)) + 3;
                    _startWaitingForMonsters = false;

                    return C.Search;
                }
                else if (_turnsLeftToWaitForFight > 0)
                {
                    _turnsLeftToWaitForFight--;
                    return C.Search;
                }
                else
                {
                    // They are not coming to us, we must go to them
                    _doneWaitingForMonsters = true;
                    return GetMoveTowards(player, monster.Position);
                }
            }
            else
            {
                if (_doneWaitingForMonsters)
                {
                    if (player.Position == _targetMonster.Position)
                    {
                        // Maybe he moved?
                        EndCombat();
                        return C.Unknown;
                    }

                    // If we can, throw something at the monster to make him mad
                    if (LinedUp(player, _targetMonster.Position))
                    {
                        player.InventoryCheck();
                        var item = GetSomethingToThrow(player);
                        player.ThrowItem(item, GetDirection(player, _targetMonster.Position));

                        StartCombat(_targetMonster);
                        return ChooseCombatMove(player, map);
                    }
                    else
                    {
                        return GetMoveTowards(player, _targetMonster.Position);
                    }
                }
                else
                {
                    var room = map.Rooms.FirstOrDefault(r => r.Player != null);
                    if (_currentChar == C.Door || room != null && room.Doors.Count > 0)
                    {
                        // Move to the nearest door away from the monster. If there are more monsters we may be screwed
                        return Flee(player, _targetMonster);
                    }
                    else
                    {
                        return GetMoveTowards(player, _targetMonster.Position);
                    }
                }
            }

            return C.Unknown;
        }

        private Monster GetTargetMonster(Player player)
        {
            if (_targetMonster == null)
            {
                return null;
            }

            var maps = player.Map.Maps;

            // Look for the monster with the same letter spreading outward from his original position
            int add = 0;
            while (add++ < 15)
            {
                for (int y = 0; y <= add; y++)
                {
                    for (var x = 0; x <= add; x++)
                    {
                        if (maps[y][x] == _targetMonster.MonsterCode)
                        {
                            _targetMonster.Position = new Position(x, y);
                            break;
                        }
                    }
                }
            }

            return _targetMonster;
        }

        private char GetDirection(Player player, Position position)
        {
            var p = player.Position;
            if (p.X < position.X) return C.Right;
            if (p.X > position.X) return C.Left;
            if (p.Y < position.Y) return C.Down;
            if (p.Y > position.Y) return C.Up;

            return C.Unknown;
        }

        private bool LinedUp(Player player, Position position)
        {
            return player.Position.X == position.X || player.Position.Y == position.Y;
        }

        private InventoryItem GetSomethingToThrow(Player player)
        {
            InventoryItem throwable = null;
            var throwables = new[] { "spear", "arrow", "wand", "staff", "bow" };
            foreach (var item in throwables)
            {
                throwable = player.InventoryItems.FirstOrDefault(i => i.Name.Contains(item, StringComparison.OrdinalIgnoreCase));
                if (throwable != null) break;
            }

            return throwable;
        }

        private void EndCombat()
        {
            _targetMonster = null;
            _combat = false;
            _doneWaitingForMonsters = false;
        }

        private double CalculateDistance(Player player, Position position)
        {
            return Math.Sqrt(Math.Pow(player.Position.X - position.X, 2) + Math.Pow(player.Position.Y - position.Y, 2));
        }

        private bool Near(Player player, char character)
        {
            var map = player.Map;
            var p = player.Position;

            var left = new Position(p.X - 1, p.Y);
            var right = new Position(p.X + 1, p.Y);
            var up = new Position(p.X, p.Y - 1);
            var down = new Position(p.X, p.Y + 1);

            if (Walkable(map, left) && map.Maps[left.Y][left.X] == character) return true;
            if (Walkable(map, right) && map.Maps[right.Y][right.X] == character) return true;
            if (Walkable(map, up) && map.Maps[up.Y][up.X] == character) return true;
            if (Walkable(map, down) && map.Maps[down.Y][down.X] == character) return true;

            return false;
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
            if (room != null && _currentChar != C.Door)
            {
                // Try to find a door that leads away from the monster
                Position bestDoor = null;
                double minDistanceToDoor = double.MaxValue;
                foreach (var door in room.Doors)
                {
                    var distanceToPlayer = Math.Sqrt(Math.Pow(playerPos.X - door.X, 2) + Math.Pow(playerPos.Y - door.Y, 2));
                    if (distanceToPlayer < minDistanceToDoor)
                    {
                        bestDoor = door;
                        minDistanceToDoor = distanceToPlayer;
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
                if (room.IsComplete(map.Maps) && (room.Doors.Count == 0 || room.Doors.Any(d => _visited.ContainsKey((d.X, d.Y)))))
                {
                    visitedRooms++;
                }
            }

            // Backup, just in case there are not enough rooms
            var beenEveryWhereTwice = _visited.Count > 4 && _visited.All(v => v.Value > 1);

            // Also ok if we visited lots of locations
            var lotsOfLocations = _visited.Count >= 250;

            return visitedRooms >= 5 || beenEveryWhereTwice || lotsOfLocations;
        }

        private Monster GetCloseMonster(Player player, Map currentMap)
        {
            var maps = player.Map.Maps;
            var p = player.Position;

            var left = new Position(p.X - 1, p.Y);
            var right = new Position(p.X + 1, p.Y);
            var up = new Position(p.X, p.Y - 1);
            var down = new Position(p.X, p.Y + 1);

            Monster GetMonsterAt(Position position)
            {
                if (Monster.Start(maps, position.X, position.Y))
                {
                    return new Monster(new Position(position.X, position.Y), maps[position.Y][position.X]);
                }

                return null;
            }

            var monster = GetMonsterAt(left);
            if (monster != null) return monster;

            monster = GetMonsterAt(right);
            if (monster != null) return monster;

            monster = GetMonsterAt(up);
            if (monster != null) return monster;

            monster = GetMonsterAt(down);
            if (monster != null) return monster;

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
            if (validMoves.Count == 1 && player.Position != _lastDeadEnd)
            {
                var pos = (player.Position.X, player.Position.Y);
                Debug.WriteLine("Dead end detected at " + pos);
                _lastDeadEnd = player.Position;
                _searchTurnsRemaining = 20;
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
            var map = player.Map;
            var p = player.Position;

            var validMoves = new List<char>();

            var left = new Position(p.X - 1, p.Y);
            var right = new Position(p.X + 1, p.Y);
            var up = new Position(p.X, p.Y - 1);
            var down = new Position(p.X, p.Y + 1);

            var upLeft = new Position(p.X - 1, p.Y - 1);
            var upRight = new Position(p.X + 1, p.Y - 1);
            var downLeft = new Position(p.X - 1, p.Y + 1);
            var downRight = new Position(p.X + 1, p.Y + 1);

            // Optimization, if we are on . we can move to . diagonally
            if (_currentChar == C.Floor)
            {
                if (target.X < p.X && target.Y < p.Y && map.Maps[upLeft.Y][upLeft.X] == C.Floor) return C.UpLeft;
                if (target.X > p.X && target.Y < p.Y && map.Maps[upRight.Y][upRight.X] == C.Floor) return C.UpRight;
                if (target.X < p.X && target.Y > p.Y && map.Maps[downLeft.Y][downLeft.X] == C.Floor) return C.DownLeft;
                if (target.X > p.X && target.Y > p.Y && map.Maps[downRight.Y][downRight.X] == C.Floor) return C.DownRight;
            }

            // Simple greedy movement
            if (target.X < p.X && Walkable(map, left)) validMoves.Add(C.Left);
            if (target.X > p.X && Walkable(map, right)) validMoves.Add(C.Right);
            if (target.Y < p.Y && Walkable(map, up)) validMoves.Add(C.Up);
            if (target.Y > p.Y && Walkable(map, down)) validMoves.Add(C.Down);

            if (validMoves.Any())
            {
                return validMoves[Random.Shared.Next(validMoves.Count)];
            }

            // Random walkable move, or search if there are none
            validMoves.Clear();
            validMoves.Add(C.Search);

            if (Walkable(map, left)) validMoves.Add(C.Left);
            if (Walkable(map, right)) validMoves.Add(C.Right);
            if (Walkable(map, up)) validMoves.Add(C.Up);
            if (Walkable(map, down)) validMoves.Add(C.Down);

            return validMoves[Random.Shared.Next(validMoves.Count)];
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
                    if (value < 10)
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

            return C.Unknown;
        }

        public static bool Died(Map map)
        {
            return map.HasString("killed");
        }

        private Map UpdateMap(Player player, Map currentMap)
        {
            player.Update(currentMap);

            return currentMap;
        }
    }
}