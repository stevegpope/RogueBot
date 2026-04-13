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
            if (x < 0 || y <= 0 || y >= 23 || x > maps[y].Length)
                return false;

            var itemCodes = new[] { C.Money, C.Wand, C.Ring, C.Potion, C.Goal, C.Armor, C.Food, C.Weapon };
            return itemCodes.Contains(maps[y][x]);
        }

        public static string ParseItemName(string details)
        {
            var knownDetails = new Dictionary<string, string>()
            {
                { "Who?", "confusion" },
                { "glows blue", "enchant weapon" },
                { "armor glows", "enchant armor" },
                { "sense the presence of magic", "detect magic" },
                { "watching over you", "protection" },
                { "feel better", "healing" },
                { "feel much better", "healing" },
                { "float", "levitation" },
                { "feel stronger", "strength" },
                { "identify scroll", "identify" },
                { "armor is covered by a shimmering", "enchant armor" },
                { "warm all over", "restore strength" },
                { "darkness falls", "blindness" },
                { "dark!", "blindness" },
                { "water hits you", "rust" },
                { "asleep", "sleep" },
                { "is dark", "blindness" },
                { "glow red", "monster confusion" },
                { "glowing red", "monster confusion" },
                { "smell food", "detect food" },
                { "nose tingles", "detect food" },
                { "gush of water", "rust" },
                { "taste", "food" },
                { "yummy", "food" },
                { "map on it", "mapping" },
                { "freeze", "hold monster" },
                { "glows and then fades", "light" },
                { "room is lit", "light" },
                { "rust vanishes", "protect armor" },
                { "turns to dust", "monster fear" },
                { "juice", "detect invisible" },
                { "blank", "blank paper" },
                { "trippy", "confusion" },
                { "feel greedy", "detect gold" },
                { "can't move", "paralysis" },
                { "pull downward", "detect gold" },
                { "Universal", "remove curse" },
                { "sick", "poison" },
                { "moving much faster", "haste" },
                { "strange feeling", "detect monster" },
                { "tingling feeling", "drain life" },
                { "genocide", "genocide" },
                { "anguish in the distance", "create monster" },
                { "humming noise", "aggravate monsters" },
                { "maniacal laughter", "monster fear" },
                { "presence of magic", "detect magic" },
                { "skillful", "raise level" },
                { "hands begin to glow", "enchant weapon" },
                { "armor is covered", "enchant armor" },
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