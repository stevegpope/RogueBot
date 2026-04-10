namespace RogueBot
{
    internal class Inventory
    {
        public static IEnumerable<InventoryItem> Get(nint console)
        {
            ConsoleController.SendKey(C.Inventory);

            var lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            var items = InventoryItem.Parse(lines);

            while (items.Count == 0)
            {
                Thread.Sleep(100);
                lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
                items = InventoryItem.Parse(lines);
            } 

            ConsoleController.SendKey(C.Space);

            return items;
        }
    }
}
