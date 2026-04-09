namespace RogueBot
{
    internal class Map
    {
        public HashSet<Room> Rooms { get; private set; }
        public List<string> Maps { get; private set; }
        public Position Player { get; set; }

        internal Map(List<string> maps)
        {
            Rooms = new HashSet<Room>();
            Maps = maps;
            ParseMap();
        }

        private void ParseMap() 
        { 
            for (var y = 0; y < Maps.Count; y++)
            {
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

        internal void Shift(Map previousMap)
        {
            // Shift relative to the room closest to center of the map
            var center = new Position(Maps[0].Length / 2, Maps.Count / 2);

            var previousCenterRoom = previousMap.Rooms.OrderBy(r => CalculateDistance(r.TopLeft, center)).FirstOrDefault();
            var centerRoom = Rooms.OrderBy(r => CalculateDistance(r.TopLeft, center)).FirstOrDefault();
            if (previousCenterRoom == null || centerRoom == null)
            {
                return;
            }

            if (!previousCenterRoom.TopLeft.Equals(centerRoom.TopLeft))
            {
                // Shift the map so that the center room is in the same position as the previous center room
                var shiftX = previousCenterRoom.TopLeft.X - centerRoom.TopLeft.X;
                var shiftY = previousCenterRoom.TopLeft.Y - centerRoom.TopLeft.Y;

                for(var y = 0; y < Maps.Count; y++)
                {
                    for(var x = 0; x < Maps[y].Length; x++)
                    {
                        var newX = x + shiftX;
                        var newY = y + shiftY;
                        if (newX >= 0 && newX < Maps[y].Length && newY >= 0 && newY < Maps.Count)
                        {
                            Maps[y] = Maps[y].Remove(x, 1).Insert(x, Maps[newY][newX].ToString());
                        }
                        else
                        {
                            // Out of bounds, set to empty
                            Maps[y] = Maps[y].Remove(x, 1).Insert(x, C.Space.ToString());
                        }
                    }
                }

                ParseMap();
            }
        }

        private double CalculateDistance(Position topLeft, Position center)
        {
            // Calculate the distance between two positions using the Euclidean distance formula
            return Math.Sqrt(Math.Pow(topLeft.X - center.X, 2) + Math.Pow(topLeft.Y - center.Y, 2));
        }
    }
}
