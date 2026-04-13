using System.Text;

namespace RogueBot
{
    public class Map
    {
        public HashSet<Room> Rooms { get; set; }
        public char[][] Maps { get; set; }
        public Position Player { get; set; }
        public string Details { get; set; }

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
            SaveDetails();
        }

        private void SaveDetails()
        {
            Details = new string(Maps[0]);

            if (string.IsNullOrWhiteSpace(Details))
                return;

            List<string> lines;

            const string fileName = "details.txt";
            if (File.Exists(fileName))
            {
                lines = File.ReadAllLines(fileName).ToList();
            }
            else
            {
                lines = new List<string>();
            }

            // Add newest entry at top
            lines.Insert(0, Details);

            // Keep only last 1000
            if (lines.Count > 1000)
            {
                lines = lines.Take(1000).ToList();
            }

            File.WriteAllLines(fileName, lines);
        }

        private void ParseMap() 
        { 
            for (var y = 0; y < Maps.Length; y++)
            {
                var line = new string(Maps[y]);

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
