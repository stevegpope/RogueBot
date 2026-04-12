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

                Map previousMap = null;
                Map currentMap = null;
                Room startRoom = null;
                char previousMove = '\0';

                var console = ConsoleController.GetConsole(rogue);

                while (!rogue.HasExited)
                {
                    try
                    {
                        currentMap = ConsoleController.WaitForTurnReady(console);
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
                        ConsoleController.SendKey(move);
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

                Console.WriteLine($"Started new Rogue PID: {result.Id}");
                Thread.Sleep(1000);
            }

            return result;
        }
    }
}