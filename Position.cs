namespace RogueBot
{
    public class Position : IEquatable<Position>
    {
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public override int GetHashCode()
        {
            return $"${X},{Y}".GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is Position other)
            {
                return Equals(other);
            }

            return base.Equals(obj);
        }

        public bool Equals(Position? other)
        {
            return other != null && X == other.X && Y == other.Y;
        }

        public static bool operator ==(Position? left, Position? right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Position? left, Position? right)
        {
            return !(left == right);
        }
    }
}