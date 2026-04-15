namespace RogueBot
{
    public class Monster
    {
        public Position Position { get; set; }
        public char MonsterCode { get; }

        public Monster(Position position, char code)
        {
            Position = position;
            MonsterCode = code;
        }


        public static bool Start(char[][] maps, int x, int y)
        {
            if (x < 0 || y <= 0 || y >= 23 || x > maps[y].Length)
                return false;

            return char.IsAsciiLetter(maps[y][x]);
        }
    }
}