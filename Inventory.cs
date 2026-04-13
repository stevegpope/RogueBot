namespace RogueBot
{
    internal class Inventory
    {
        public static List<InventoryItem> Get(nint console)
        {
            ConsoleController.SendKey(C.Inventory);
            Thread.Sleep(250);

            var lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            var items = InventoryItem.Parse(lines);

            while (items.Count == 0)
            {
                Thread.Sleep(500);
                lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
                items = InventoryItem.Parse(lines);
            } 

            ConsoleController.SendKey(C.Space);
            Thread.Sleep(250);

            return items;
        }
    }
}
