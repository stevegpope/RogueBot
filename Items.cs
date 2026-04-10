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
            var itemCodes = new[] { C.Money, C.Wand, C.Ring, C.Potion, C.Goal, C.Armor, C.Food, C.Scroll, C.Weapon };
            return itemCodes.Contains(maps[y][x]);
        }
    }
}