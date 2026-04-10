using System.ComponentModel;
using System.Diagnostics;

namespace RogueBot
{
    public class Room
    {
        public Position TopLeft { get; set; }
        public HashSet<Position> Doors { get; private set; }
        public HashSet<Items> ItemSet { get; private set; }
        public HashSet<Monsters> MonsterSet { get; private set; }
        public Position StairsDown = null;
        public Position StairsUp = null;
        public Position Player = null;

        public int Width { get; set; }
        public int Height { get; set; }

        public Room(Position topLeft, char[][] maps)
        {
            TopLeft = topLeft;

            Doors = new HashSet<Position>();
            ItemSet = new HashSet<Items>();
            MonsterSet = new HashSet<Monsters>();

            ParseRoom(maps);
        }

        private void ParseRoom(char[][] maps)
        {
            CalculateSize(maps);
            var expected = new[] { C.WallTop, C.WallSide, C.Door, C.Floor, C.Player, C.Space, C.Trap };
            for (var y = TopLeft.Y; y < TopLeft.Y + Height; y++)
            {
                for (var x = TopLeft.X; x < TopLeft.X + Width; x++)
                {
                    if (maps[y][x] == C.Door)
                    {
                        Doors.Add(new Position(x, y));
                    }
                    else if (Items.Start(maps, x, y))
                    {
                        ItemSet.Add(new Items(new Position(x, y), maps[y][x]));
                    }
                    else if (Monsters.Start(maps, x, y))
                    {
                        MonsterSet.Add(new Monsters(new Position(x, y), maps[y][x]));
                    }
                    else if (maps[y][x] == C.StairsDown)
                    {
                        StairsDown = new Position(x, y);
                    }
                    else if (maps[y][x] == C.Player)
                    {
                        Player = new Position(x, y);
                    }
                    else if (expected.Contains(maps[y][x])) {
                        continue;
                    } 
                    else
                    {
                        var code = maps[y][x];
                        Debug.WriteLine($"Unexpected character {code} at position ({x}, {y})");
                    }
                }
            }
        }

        private void CalculateSize(char[][] maps)
        {
            // Width
            var topPieces = new[] { C.WallTop, C.Door };
            for (var x = TopLeft.X; x < maps[TopLeft.Y].Length; x++)
            {
                if (topPieces.Contains(maps[TopLeft.Y][x]))
                {
                    Width++;
                }
                else
                {                     
                    break;
                }
            }

            // Height
            var sidePieces = new[] { C.WallSide, C.Door, C.WallTop };
            for (var y = TopLeft.Y; y < maps.Length; y++)
            {
                var code = maps[y][TopLeft.X];
                if (sidePieces.Contains(code))
                {
                    Height++;
                }
                else
                {
                    break;
                }
            }
        }

        public static bool Start(char[][] maps, int x, int y)
        {
            char c = maps[y][x];
            char previous = x > 0 ? maps[y][x - 1] : C.Space;
            return c == C.WallTop && previous == C.Space;
        }

        public Position ChooseTarget(Player player, char? currentChar)
        {
            // Any items?
            if (ItemSet.Any())
            {
                var item = ItemSet.First();
                return item.Position;
            }
            else if (MonsterSet.Any())
            {
                // Move towards the first monster
                var item = MonsterSet.First();
                return item.Position;
            }
            else if (StairsDown != null)
            {
                // Move towards the stairs
                return StairsDown;
            }

            return null;
        }

        internal bool IsComplete(Player player)
        {
            // A room is complete if all the walls and doors have been discovered
            var startX = TopLeft.X;
            var startY = TopLeft.Y;
            var map = player.Map.Maps;

            // Top
            for (var x = startX; x < startX + Width; x++)
            {
                var c = map[startY][x];
                if (c == C.Space)
                {
                    return false;
                }
            }

            // Bottom
            for (var x = startX; x < startX + Width; x++)
            {
                var c = map[startY + Height - 1][x];
                if (c == C.Space)
                {
                    return false;
                }
            }

            // Left
            for (var y = startY; y < startY + Height; y++)
            {
                var c = map[y][startX];
                if (c == C.Space)
                {
                    return false;
                }
            }

            // Right
            for (var y = startY; y < startY + Height; y++)
            {
                var c = map[y][startX + Width - 1];
                if (c == C.Space)
                {
                    return false;
                }
            }

            return true;
        }
    }
}