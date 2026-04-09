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


        internal static bool Start(List<string> maps, int x, int y)
        {
            return char.IsAsciiLetter(maps[y][x]);
        }
    }
}