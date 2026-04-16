namespace RogueBot
{
    internal class Inventory
    {
        public static List<InventoryItem> Get(ConsoleController console)
        {
            console.SendKey(C.Inventory);
            Thread.Sleep(250);

            var lines = console.ReadMap().Select(line => new string(line)).ToList();
            var items = InventoryItem.Parse(lines);

            while (items.Count == 0)
            {
                Thread.Sleep(500);
                lines = console.ReadMap().Select(line => new string(line)).ToList();
                items = InventoryItem.Parse(lines);
            }

            console.SendKey(C.Space);
            Thread.Sleep(250);

            return items;
        }
    }
}
