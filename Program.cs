using System.Diagnostics;

namespace RogueBot
{
    class Program
    {
        [STAThread] // Required for SendKeys
        static void Main()
        {
            while (true)
            {
                var rogue = RunRogue();

                var logic = new Logic();
                Player player = null;

                var console = new ConsoleController(rogue);

                while (!rogue.HasExited)
                {
                    try
                    {
                        var currentMap = console.WaitForTurnReady();
                        if (currentMap == null)
                        {
                            continue;
                        }

                        if (player == null)
                        {
                            player = new Player(currentMap, console);
                            player.InventoryCheck();
                        }

                        var move = currentMap.HasString(C.Player.ToString()) ? logic.ChooseMove(player, currentMap) : logic.ChooseMove(currentMap);
                        console.SendKey(move);
                    }
                    catch (Exception ex)
                    {
                        // Ignore errors if the window isn't ready yet
                        Debug.WriteLine(ex);
                    }
                }
            }
        }


        private static Process RunRogue()
        {
            const string processName = "rogue54";

            var result = Process.GetProcessesByName(processName)
                                  .FirstOrDefault();

            if (result == null || result.HasExited)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = @"rogue54.exe",
                    WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "rogue"),
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                result = Process.Start(psi);
                Thread.Sleep(1000);

                try
                {
                    Console.WriteLine($"Started new Rogue PID: {result.Id}");
                }
                catch
                {
                    Thread.Sleep(1000);
                    return RunRogue();
                }
            }

            return result;
        }
    }
}