using System.Text.RegularExpressions;

namespace RogueBot
{
    public class InventoryItem
    {
        public char Letter { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }

        public bool IsFood
        {
            get
            {
                return Name.Contains("rations", StringComparison.OrdinalIgnoreCase) ||
                       Name.Contains("bread", StringComparison.OrdinalIgnoreCase) ||
                       Name.Contains("meat", StringComparison.OrdinalIgnoreCase) ||
                       Name.Contains("fruit", StringComparison.OrdinalIgnoreCase) ||
                       Name.Contains("food", StringComparison.OrdinalIgnoreCase) ||
                       Name.Contains("slime", StringComparison.OrdinalIgnoreCase) ||
                       Name.Contains("vegetable", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsRing
        {
            get
            {
                return Name.Contains("ring", StringComparison.OrdinalIgnoreCase) &&
                    !Name.Contains("ring mail", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool IsArmor
        {
            get
            {
                return Name.Contains("armor", StringComparison.OrdinalIgnoreCase) ||
                    Name.Contains("mail", StringComparison.OrdinalIgnoreCase) ||
                    Name.Contains("helmet", StringComparison.OrdinalIgnoreCase) ||
                    Name.Contains("shield", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static readonly Regex ProtectionRegex =
            new Regex(@"\[protection\s+(?<value>-?\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public int ArmorValue
        {
            get 
            {
                var match = ProtectionRegex.Match(Name);
                if (!match.Success)
                    return 0;

                return int.Parse(match.Groups["value"].Value);
            }
        }

        public InventoryItem(char letter, string name, string status)
        {
            Letter = letter;
            Name = name;
            Status = status;
        }

        public static List<InventoryItem> Parse(List<string> lines)
        {
            var items = new List<InventoryItem>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 1. Letter
                if (line.Length < 3 || line[1] != ')')
                    continue;

                char letter = line[0];

                // 2. Everything after "a) "
                string rest = line.Substring(3).Trim();

                string name = rest;
                string status = null;

                // 3. Check for trailing "(status)"
                int statusStart = rest.LastIndexOf(" (");
                if (statusStart >= 0 && rest.EndsWith(")"))
                {
                    name = rest.Substring(0, statusStart).Trim();
                    status = rest.Substring(statusStart + 2, rest.Length - statusStart - 3).Trim();
                }

                items.Add(new InventoryItem(letter, name, status));
            }

            return items;
        }
    }
}