namespace RogueBot
{
    public class Monster
    {
        public Position Position { get; }
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

            // Assume ? is a monster
            return char.IsAsciiLetter(maps[y][x]) || maps[y][x] == '?';
        }
    }
}