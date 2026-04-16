using System.Diagnostics;

namespace RogueBot
{
    class Program
    {
        [STAThread] // Required for SendKeys
        static async Task Main()
        {
            const int Processes = 3;
            var runners = new List<Runner>();

            for (int i = 0; i < Processes; i++)
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