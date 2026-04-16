using System.Diagnostics;

namespace RogueBot
{
    class Program
    {
        [STAThread] // Required for SendKeys
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Write("Usage: RogueBot.exe <processes>");
                return;
            }

            var processes = int.Parse(args[0]);

            var runners = new List<Runner>();

            for (int i = 0; i < processes; i++)
            {
                runners.Add(new Runner(runners));
            }

            var tasks = new List<Task>();

            foreach (var runner in runners)
            {
                tasks.Add(Task.Run(() => runner.Run()));
            }

            await Task.WhenAll(tasks);
        }
    }
}