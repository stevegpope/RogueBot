using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RogueBot
{
    public class Player
    {
        private static DateTime CheckedForFoodLast = DateTime.MinValue;

        Regex regex = new Regex(
            @"Level:\s*(?<Level>\d+)\s+" +
            @"Gold:\s*(?<Gold>\d+)\s+" +
            @"Hp:\s*(?<Hp>\d+)\((?<HpMax>\d+)\)\s+" +
            @"Str:\s*(?<Str>\d+)\((?<StrMax>\d+)\)\s+" +
            @"Arm:\s*(?<Armor>\d+)\s+" +
            @"Exp:\s*(?<ExpLevel>\d+)\/(?<ExpPoints>\d+)", RegexOptions.Compiled);

        public int Level { get; private set; }
        public int Gold { get; private set; }
        public int Hp { get; private set; }
        public int HpMax { get; private set; }
        public int Str { get; private set; }
        public int StrMax { get; private set; }
        public int Armor { get; private set; }
        public int ExpLevel { get; private set; }
        public int ExpPoints { get; private set; }
        public Position Position { get; private set; }
        public Map Map { get; }
        public string State { get; private set; }

        private readonly string[] States = new[] { "Hungry", "Weak", "Faint" };

        public Player(Map map)
        {
            Map = map;
            var lines = map.Maps;

            var statusLine = lines.Select(l => new string(l)).FirstOrDefault(m => m.Contains("Hp:"));
            if (statusLine == null)
            {
                return;
            }

            State = States.FirstOrDefault(s => statusLine.Contains(s));

            Match m = regex.Match(statusLine);

            if (m.Success)
            {
                Level = int.Parse(m.Groups["Level"].Value);
                Gold = int.Parse(m.Groups["Gold"].Value);

                Hp = int.Parse(m.Groups["Hp"].Value);
                HpMax = int.Parse(m.Groups["HpMax"].Value);

                Str = int.Parse(m.Groups["Str"].Value);
                StrMax = int.Parse(m.Groups["StrMax"].Value);

                Armor = int.Parse(m.Groups["Armor"].Value);

                ExpLevel = int.Parse(m.Groups["ExpLevel"].Value);
                ExpPoints = int.Parse(m.Groups["ExpPoints"].Value);
            }

            Position = Map.Player;
        }

        internal bool Hungry()
        {
            var hungryStates = new[] { "Hungry", "Weak", "Faint" };
            return hungryStates.Any(s => State != null && State.Contains(s));
        }

        internal static void Eat(nint console)
        {
            if (CheckedForFoodLast.AddMinutes(1) > DateTime.Now)
                return;

            try
            {
                var inventory = Inventory.Get(console);

                foreach (var item in inventory)
                {
                    if (item.IsFood)
                    {
                        EatItem(console, item);
                        return;
                    }
                }
            }
            finally
            {
                CheckedForFoodLast = DateTime.Now;
            }
        }

        private static void EatItem(nint console, InventoryItem item)
        {
            ConsoleController.WaitForScreenChange(console);

            ConsoleController.SendKey(C.Eat);

            var lines = ConsoleController.WaitForText(console, "eat?");

            ConsoleController.SendKey(item.Letter);
        }

        internal static void Use(nint console, string foundStr)
        {
            var searchTerms = new[] { "ring", "sword", "potion", "scroll", "mail" };
            string itemType = null;
            foreach (var searchTerm in searchTerms)
            {
                if (foundStr.Contains(searchTerm))
                {
                    if (foundStr.Contains("ring mail", StringComparison.OrdinalIgnoreCase))
                    {
                        itemType = "mail";
                    }
                    else
                    {
                        itemType = searchTerm;

                    }

                    break;
                }
            }

            if (itemType == null)
                return;

            Debug.WriteLine($"Use found {itemType} in '{foundStr}'");
            var inventory = Inventory.Get(console);

            // Assume more powerful items are later
            var newItem = inventory.LastOrDefault(i => i.Name.Contains(itemType) && i.Status == null);

            switch (itemType)
            {
                case "ring":
                    WearRing(console, newItem, inventory);
                    break;
                case "sword":
                    WieldSword(console, newItem);
                    break;
                case "potion":
                    DrinkPotion(console, newItem);
                    break;
                case "scroll":
                    ReadScroll(console, newItem); 
                    break;
                case "mail":
                    WearArmor(console, newItem);
                    break;
                default:
                    throw new Exception("Unknown item type: " + itemType);
            }
        }

        private static void WearArmor(nint console, InventoryItem? newItem)
        {
            Debug.WriteLine($"Trying to wear armor {newItem.Name} ({newItem.Letter})");

            ConsoleController.SendKey(C.TakeOff);

            ConsoleController.SendKey(C.WearArmor);
            ConsoleController.WaitForText(console, "Which object");
            
            ConsoleController.SendKey(newItem.Letter);
            FinishUsingItem(console, "armor");
        }

        private static void ReadScroll(nint console, InventoryItem? newItem)
        {
            Debug.WriteLine($"Trying to read {newItem.Name} ({newItem.Letter})");

            ConsoleController.SendKey(C.ReadScroll);
            ConsoleController.WaitForText(console, "Which object");

            ConsoleController.SendKey(newItem.Letter);
            FinishUsingItem(console, "scroll");
        }

        private static void DrinkPotion(nint console, InventoryItem? newItem)
        {
            Debug.WriteLine($"Trying to quaff {newItem.Name} ({newItem.Letter})");

            ConsoleController.SendKey(C.QuaffPotion);
            ConsoleController.WaitForText(console, "Which object");

            ConsoleController.SendKey(newItem.Letter);
            FinishUsingItem(console, "potion");
        }

        private static void WieldSword(nint console, InventoryItem? newItem)
        {
            Debug.WriteLine($"Trying to wield {newItem.Name} ({newItem.Letter})");

            ConsoleController.SendKey(C.Wield);
            ConsoleController.WaitForText(console, "Which object");

            ConsoleController.SendKey(newItem.Letter);
            FinishUsingItem(console, "sword");
        }

        private static void WearRing(nint console, InventoryItem newItem, IEnumerable<InventoryItem> inventory)
        {
            Debug.WriteLine($"Trying to wear ring {newItem.Name} ({newItem.Letter})");

            // Look for open ring slot
            var left = inventory.FirstOrDefault(i => i.Status?.Contains("left hand") == true);
            var right = inventory.FirstOrDefault(i => i.Status?.Contains("right hand") == true);
            if (left != null && right != null)
            {
                ConsoleController.SendKey(C.Remove);
                ConsoleController.WaitForText(console, "hand");

                // Remove a random one
                var toRemove = new[] { left, right }[new Random().Next(2)];
                ConsoleController.SendKey(toRemove.Letter);
                ConsoleController.WaitForText(console, "Was wearing", "cursed");
            }

            ConsoleController.SendKey(C.PutOn);
            ConsoleController.WaitForText(console, "Which object");
            ConsoleController.SendKey(newItem.Letter);

            if (left == null && right == null)
            {
                // Left hand
                ConsoleController.SendKey("l");
                FinishUsingItem(console, "ring");
            }
        }

        private static void FinishUsingItem(nint console, string name)
        {
            Thread.Sleep(500);

            var map = new Map(ConsoleController.ReadMap(console));
            while (map.HasString("more"))
            {
                Debug.WriteLine("MORE");
                ConsoleController.SendKey(C.Space);
                Thread.Sleep(500);
                map = new Map(ConsoleController.ReadMap(console));
            }

            Thread.Sleep(1000);

            if (map.HasString("call it"))
            {
                Debug.WriteLine("Naming");
                ConsoleController.SendKey(name + " " + Random.Shared.NextDouble());
                ConsoleController.SendKey("{ENTER}");
                Thread.Sleep(500);
            }

            if (map.HasString("identify"))
            {
                Debug.WriteLine("Identifying");

                map = new Map(ConsoleController.ReadMap(console));
                while (map.HasString("more"))
                {
                    ConsoleController.SendKey(C.Space);
                    Thread.Sleep(250);
                    map = new Map(ConsoleController.ReadMap(console));
                }

                ConsoleController.SendKey("*");
                Thread.Sleep(500);

                map = new Map(ConsoleController.ReadMap(console));
                if (map.HasString("appropriate"))
                {
                    ConsoleController.SendKey(C.Space);
                    Thread.Sleep(500);
                }
                else
                {
                    Debug.WriteLine("Identify list present");
                    var lines = map.Maps.Select(line => new string(line)).ToList();
                    var items = InventoryItem.Parse(lines);

                    while (map.HasString("more"))
                    {
                        ConsoleController.SendKey(C.Space);
                        Thread.Sleep(250);
                        map = new Map(ConsoleController.ReadMap(console));
                    }

                    if (items.Any())
                    {
                        var item = items.First();
                        ConsoleController.SendKey(item.Letter);
                        FinishUsingItem(console, item.Name);
                    }
                }
            }

            Debug.WriteLine("Finished using " + name);
        }

        internal static void InventoryCheck(nint console)
        {
            var inventory = Inventory.Get(console);
            var currentArmor = inventory.FirstOrDefault(i => i.IsArmor && i.Status != null);

            foreach (var item in inventory)
            {
                if (item.Name.Contains("potion"))
                {
                    DrinkPotion(console, item);
                }
                else if (item.Name.Contains("scroll"))
                {
                    ReadScroll(console, item);
                }
                else if (item.Name.Contains("sword") && item.Status == null)
                {
                    WieldSword(console, item);
                }
                else if (item.IsRing && item.Status == null)
                {
                    WearRing(console, item, inventory);
                }
                else if (item.IsArmor && item.Status == null)
                {
                    if (currentArmor != null && item.ArmorValue > currentArmor.ArmorValue)
                    {
                        WearArmor(console, item);
                    }
                }
            }
        }
    }
}
