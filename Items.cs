namespace RogueBot
{
    public class Items
    {
        public Position Position { get; }
        public char ItemCode { get; }

        public Items(Position position, char code)
        {
            Position = position;
            ItemCode = code;
        }

        public static bool Start(char[][] maps, int x, int y)
        {
            var itemCodes = new[] { C.Money, C.Wand, C.Ring, C.Potion, C.Goal, C.Armor, C.Food, C.Weapon };
            return itemCodes.Contains(maps[y][x]);
        }

        public static string ParseItemName(string details)
        {
            var knownDetails = new Dictionary<string, string>()
            {
                { "Who?", "confusion" },
                { "glows blue", "enchant weapon" },
                { "glows silver", "enchant armor" },
                { "sense the presence of magic", "detect magic" },
                { "watching over you", "protection" },
                { "feel better", "healing" },
                { "float in the air", "levitation" },
                { "feel stronger", "strength" },
                { "identify scroll", "identify" },
                { "armor is covered by a shimmering", "enchant armor" },
                { "Universal", "remove curse" },
                { "warm all over", "restore strength" },
                { "darkness falls", "blindness" },
                { "water hits you", "rust" },
                { "asleep", "sleep" },
                { "is dark", "blindness" },
            };

            foreach (var kvp in knownDetails)
            {
                var name = kvp.Key;
                if (details.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}