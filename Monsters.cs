namespace RogueBot
{
    public class Monsters
    {
        public Position Position { get; }
        public char MonsterCode { get; }

        public Monsters(Position position, char code)
        {
            Position = position;
            MonsterCode = code;
        }


        public static bool Start(char[][] maps, int x, int y)
        {
            return char.IsAsciiLetter(maps[y][x]);
        }
    }
}