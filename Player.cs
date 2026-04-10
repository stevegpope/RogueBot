using System.Text.RegularExpressions;

namespace RogueBot
{
    public class Player
    {
        Regex regex = new Regex(
            @"Level:\s*(?<Level>\d+)\s+" +
            @"Gold:\s*(?<Gold>\d+)\s+" +
            @"Hp:\s*(?<Hp>\d+)\((?<HpMax>\d+)\)\s+" +
            @"Str:\s*(?<Str>\d+)\((?<StrMax>\d+)\)\s+" +
            @"Arm:\s*(?<Armor>\d+)\s+" +
            @"Exp:\s*(?<ExpLevel>\d+)\/(?<ExpPoints>\d+)", RegexOptions.Compiled);

        public int Level { get; private set; }
        public int Gold { get; private set; }
        public int Hp { get; private set; }
        public int HpMax { get; private set; }
        public int Str { get; private set; }
        public int StrMax { get; private set; }
        public int Armor { get; private set; }
        public int ExpLevel { get; private set; }
        public int ExpPoints { get; private set; }
        public Position Position { get; set; }
        public Map Map { get; }

        public Player(Map map)
        {
            Map = map;
            var lines = map.Maps;

            var statusLine = lines.Select(l => new string(l)).FirstOrDefault(m => m.Contains("Hp:"));
            if (statusLine == null)
            {
                return;
            }

            Match m = regex.Match(statusLine);

            if (m.Success)
            {
                Level = int.Parse(m.Groups["Level"].Value);
                Gold = int.Parse(m.Groups["Gold"].Value);

                Hp = int.Parse(m.Groups["Hp"].Value);
                HpMax = int.Parse(m.Groups["HpMax"].Value);

                Str = int.Parse(m.Groups["Str"].Value);
                StrMax = int.Parse(m.Groups["StrMax"].Value);

                Armor = int.Parse(m.Groups["Armor"].Value);

                ExpLevel = int.Parse(m.Groups["ExpLevel"].Value);
                ExpPoints = int.Parse(m.Groups["ExpPoints"].Value);
            }

            Position = Map.Player;
            if (Position == null)
            {
                Console.WriteLine("Player position not found in map.");
            }
        }
    }
}
