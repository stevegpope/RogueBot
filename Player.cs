using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RogueBot
{
    public class Player
    {
        private DateTime CheckedForFoodLast = DateTime.MinValue;

        Regex regex = new Regex(
            @"Level:\s*(?<Level>\d+)\s+" +
            @"Gold:\s*(?<Gold>\d+)\s+" +
            @"Hp:\s*(?<Hp>\d+)\((?<HpMax>\d+)\)\s+" +
            @"Str:\s*(?<Str>\d+)\((?<StrMax>\d+)\)\s+" +
            @"Arm:\s*(?<Armor>\d+)\s+" +
            @"Exp:\s*(?<ExpLevel>\d+)\/(?<ExpPoints>\d+)", RegexOptions.Compiled);

        private ConsoleController _console;

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

        private int _pid;

        public Map Map { get; private set; }
        public string State { get; private set; }
        public List<InventoryItem> InventoryItems { get; private set; }

        private readonly string[] States = new[] { "Hungry", "Weak", "Faint" };


        public Player(Map map, ConsoleController console)
        {
            _console = console;
            Update(_console.Pid, map);
        }

        internal bool Hungry()
        {
            var hungryStates = new[] { "Hungry", "Weak", "Faint" };
            return hungryStates.Any(s => State != null && State.Contains(s));
        }

        internal void Eat()
        {
            if (CheckedForFoodLast.AddSeconds(5) > DateTime.Now)
                return;

            Thread.Sleep(500);
            InventoryItems = Inventory.Get(_console);

            try
            {
                foreach (var item in InventoryItems)
                {
                    if (item.IsFood)
                    {
                        if (item.IsPotion)
                        {
                            // Food potion. Yes, really.
                            QuaffPotion(item);
                        }
                        else
                        {
                            EatItem(item);
                        }

                        return;
                    }
                }
            }
            finally
            {
                CheckedForFoodLast = DateTime.Now;
            }
        }

        private void EatItem(InventoryItem item)
        {
            _console.WaitForTurnReady();

            _console.SendKey(C.Eat);
            Thread.Sleep(1000);

            var lines = _console.WaitForText("eat?");

            _console.SendKey(item.Letter);
        }

        internal void Use(string foundStr)
        {
            C.WriteLine(_pid, $"Try to use {foundStr}");
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

            C.WriteLine(_pid, $"Use found {itemType} in '{foundStr}'");
            InventoryItems = Inventory.Get(_console);

            // Assume more powerful items are later
            var newItem = InventoryItems.LastOrDefault(i => i.Name.Contains(itemType) && i.Status == null);
            if (newItem == null)
            {
                C.WriteLine(_pid, "Weird stuff, not there?");
                return;
            }

            if (newItem.Identified && (newItem.Name.Contains("potion") || newItem.Name.Contains("scroll")))
            {
                // Do not use known consumables
                return;
            }

            switch (itemType)
            {
                case "ring":
                    WearRing(newItem);
                    break;
                case "sword":
                    WieldSword(newItem);
                    break;
                case "potion":
                    QuaffPotion(newItem);
                    break;
                case "scroll":
                    ReadScroll(newItem); 
                    break;
                case "mail":
                    WearArmor(newItem);
                    break;
                default:
                    throw new Exception("Unknown item type: " + itemType);
            }
        }

        private void WearArmor(InventoryItem? newItem)
        {
            C.WriteLine(_pid, $"Trying to wear armor {newItem.Name} ({newItem.Letter})");

            _console.SendKey(C.TakeOff);
            Thread.Sleep(500);

            _console.SendKey(C.WearArmor);
            Thread.Sleep(500);

            _console.WaitForText("Which object");
            _console.SendKey(newItem.Letter);

            FinishUsingItem("armor");
        }

        internal void ReadScroll(InventoryItem? newItem)
        {
            C.WriteLine(_pid, $"Trying to read {newItem.Name} ({newItem.Letter})");

            _console.SendKey(C.ReadScroll);
            _console.WaitForText( "Which object");

            _console.SendKey(newItem.Letter);
            InventoryItems.Remove(newItem);
            FinishUsingItem("scroll");
        }

        internal void QuaffPotion(InventoryItem? newItem)
        {
            C.WriteLine(_pid, $"Trying to quaff {newItem.Name} ({newItem.Letter})");

            _console.SendKey(C.QuaffPotion);
            _console.WaitForText("Which object");

            _console.SendKey(newItem.Letter);
            InventoryItems.Remove(newItem);
            FinishUsingItem("potion");
        }

        private void WieldSword(InventoryItem? newItem)
        {
            C.WriteLine(_pid, $"Trying to wield {newItem.Name} ({newItem.Letter})");

            _console.SendKey(C.Wield);
            _console.WaitForText("Which object");

            _console.SendKey(newItem.Letter);
            FinishUsingItem("sword");
        }

        private void WearRing(InventoryItem newItem)
        {
            C.WriteLine(_pid, $"Trying to wear ring {newItem.Name} ({newItem.Letter})");

            // Look for open ring slot
            var left = InventoryItems.FirstOrDefault(i => i.Status?.Contains("left hand") == true);
            var right = InventoryItems.FirstOrDefault(i => i.Status?.Contains("right hand") == true);
            if (left != null && right != null)
            {
                _console.SendKey(C.Remove);
                _console.WaitForText("hand");

                // Remove a random one
                var toRemove = new[] { "l", "r" }[new Random().Next(2)];
                _console.SendKey(toRemove);
                _console.WaitForText("Was wearing", "cursed");
            }

            _console.SendKey(C.PutOn);
            _console.WaitForText("Which object");
            _console.SendKey(newItem.Letter);

            if (left == null && right == null)
            {
                // Left hand
                _console.SendKey("l");
                FinishUsingItem("ring");
            }
        }

        private void FinishUsingItem(string itemType)
        {
            Thread.Sleep(500);

            var itemFullName = itemType + " of " + Random.Shared.NextDouble();

            var map = new Map(_console.ReadMap());
            if (map.HasString("more"))
            {
                var itemName = Items.ParseItemName(map.Details);
                if (itemName != null)
                {
                    itemFullName = $"{itemType} of {itemName}";
                }

                C.WriteLine(_pid, "MORE");
                _console.SendKey(C.Space);
                Thread.Sleep(500);
                map = new Map(_console.ReadMap());
            }

            Thread.Sleep(1000);

            if (map.HasString("call it"))
            {
                C.WriteLine(_pid, $"Naming {itemFullName}");
                _console.SendKey(itemFullName);
                _console.SendKey(C.Enter);
                Thread.Sleep(500);
            }

            if (map.HasString("identify"))
            {
                Identify();
            }

            C.WriteLine(_pid, "Finished using " + itemType);
        }

        public void Identify()
        {
            C.WriteLine(_pid, "Identifying");

            Map = new Map(_console.ReadMap());
            while (Map.HasString("more"))
            {
                _console.SendKey(C.Space);
                Thread.Sleep(500);
                Map = new Map(_console.ReadMap());
            }

            C.WriteLine(_pid, "Open list");
            _console.SendKey("*");
            Thread.Sleep(1000);

            Map = new Map(_console.ReadMap());
            if (Map.HasString("appropriate"))
            {
                C.WriteLine(_pid, "Nothing appropriate");
                _console.SendKey(C.Space);
                Thread.Sleep(500);
            }
            else
            {
                C.WriteLine(_pid, "Identify list present");
                var lines = Map.Maps.Select(line => new string(line)).ToList();
                var items = InventoryItem.Parse(lines);

                C.WriteLine(_pid, "Close list");
                _console.SendKey(C.Space);
                Thread.Sleep(500);

                C.WriteLine(_pid, "Choose item to ID");
                if (items.Any())
                {
                    var item = items.Last();
                    _console.SendKey(item.Letter);
                    C.WriteLine(_pid, "Finish up");
                    FinishUsingItem(item.Name);
                }
            }
        }

        internal void InventoryCheck()
        {
            InventoryItems = Inventory.Get(_console);
            var currentArmor = InventoryItems.FirstOrDefault(i => i.IsArmor && i.Status != null);

            var items = new List<InventoryItem>(InventoryItems);
            foreach (var item in items)
            {
                if (item.Name.Contains("potion") && !item.Identified)
                {
                    QuaffPotion(item);
                }
                else if (item.Name.Contains("scroll") && !item.Identified)
                {
                    ReadScroll(item);
                }
                else if (item.Name.Contains("sword") && item.Status == null)
                {
                    WieldSword(item);
                }
                else if (item.IsRing && item.Status == null)
                {
                    WearRing(item);
                }
                else if (item.IsArmor && item.Status == null)
                {
                    if (currentArmor != null && item.ArmorValue > currentArmor.ArmorValue)
                    {
                        WearArmor(item);
                    }
                }
            }
        }

        internal void Update(int pid, Map map)
        {
            _pid = pid;

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

        internal void ThrowItem(InventoryItem item, char direction)
        {
            C.WriteLine(_pid, $"Throw ${item.Name} at ${direction}");
            _console.SendKey(C.Throw);
            Thread.Sleep(500);

            _console.SendKey(direction);
            Thread.Sleep(500);

            _console.SendKey(item.Letter);
            Thread.Sleep(500);
        }
    }
}
