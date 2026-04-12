using System.Text;

namespace RogueBot
{
    public class Map
    {
        public HashSet<Room> Rooms { get; set; }
        public char[][] Maps { get; set; }
        public Position Player { get; set; }
        public int StatusLine { get; set; }

        public Map(char[][] maps)
        {
            Rooms = new HashSet<Room>();

            // Our map is the given one, minus the bottom line which contains the status info
            Maps = new char[maps.Length - 1][];
            for (var i = 0; i < maps.Length - 1; i++)
            {
                Maps[i] = maps[i];
            }

            ParseMap();
        }

        private void ParseMap() 
        { 
            for (var y = 0; y < Maps.Length; y++)
            {
                var line = new string(Maps[y]);
                if (line.Contains("Hp: ")) StatusLine = y;

                for (var x = 0; x < Maps[y].Length; x++)
                {
                    if (Room.Start(Maps, x, y))
                    {
                        // New room
                        Rooms.Add(new Room(new Position(x, y), Maps));
                    }
                    else if (Maps[y][x] == C.Player)
                    {
                        Player = new Position(x, y);
                    }
                }
            }
        }

        public void Shift(Map previousMap, Position topLeft)
        {
            // Shift relative to the room closest to the specified topLeft position

            var previousStartRoom = previousMap.Rooms.OrderBy(r => CalculateDistance(r.TopLeft, topLeft)).FirstOrDefault();
            var closestRoom = Rooms.OrderBy(r => CalculateDistance(r.TopLeft, topLeft)).FirstOrDefault();
            if (previousStartRoom == null || closestRoom == null)
            {
                return;
            }

            if (!previousStartRoom.TopLeft.Equals(closestRoom.TopLeft))
            {
                // Shift the map so that the center room is in the same position as the previous center room
                var shiftX = previousStartRoom.TopLeft.X - closestRoom.TopLeft.X;
                var shiftY = previousStartRoom.TopLeft.Y - closestRoom.TopLeft.Y;

                for(var y = 0; y < Maps.Length; y++)
                {
                    for(var x = 0; x < Maps[y].Length; x++)
                    {
                        var newX = x + shiftX;
                        var newY = y + shiftY;
                        if (newX >= 0 && newX < Maps[y].Length && newY >= 0 && newY < Maps.Length)
                        {
                            Maps[y][x] = Maps[newY][newX];
                        }
                        else
                        {
                            // Out of bounds, set to empty
                            Maps[y][x] = C.Space;
                        }
                    }
                }

                ParseMap();
            }
        }

        private double CalculateDistance(Position topLeft, Position center)
        {
            // Calculate the distance between two positions using the Euclidean distance formula
            return Math.Sqrt(Math.Pow(topLeft.X - center.X, 2) + Math.Pow(topLeft.Y - center.Y, 2));
        }

        public bool HasString(string search)
        {
            return Maps.Any(m => new string(m).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        public string GetString(string search)
        {
            foreach (var line in Maps)
            { 
                var str = new string(line);
                if (str.Contains(search))
                {
                    return str;
                }
            }

            return null;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var line in Maps)
            {
                builder.AppendLine(new string(line));
                builder.Append("\n");
            }
            return builder.ToString();
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
