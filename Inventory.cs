namespace RogueBot
{
    internal class Inventory
    {
        public static IEnumerable<InventoryItem> Get(nint console)
        {
            SendKeys.SendWait(C.Inventory.ToString());

            var lines = ConsoleController.WaitForText(console, "worn");

            var items = InventoryItem.Parse(lines);

            SendKeys.SendWait(C.Space.ToString());

            return items;
        }
    }
}
