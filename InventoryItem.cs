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
                       Name.Contains("vegetable", StringComparison.OrdinalIgnoreCase);
            }
        }

        public InventoryItem(char letter, string name, string status)
        {
            Letter = letter;
            Name = name;
            Status = status;
        }

        private static readonly Regex ItemRegex = new Regex(
            @"^(?<letter>[a-z])\)\s+(?<name>.*?)(?:\s+\((?<status>.*?)\))?$",
            RegexOptions.Compiled
        );

        public static List<InventoryItem> Parse(List<string> lines)
        {
            var items = new List<InventoryItem>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var match = ItemRegex.Match(line);
                if (!match.Success)
                    continue;

                var letter = match.Groups["letter"].Value[0];
                var name = match.Groups["name"].Value.Trim();
                var status = match.Groups["status"].Success
                    ? match.Groups["status"].Value.Trim()
                    : null;

                items.Add(new InventoryItem(letter, name, status));
            }

            return items;
        }
    }
}