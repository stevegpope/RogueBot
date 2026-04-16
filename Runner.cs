using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RogueBot
{
    public class Runner
    {
        private List<Runner> _runners;
        private Process _rogue = null;

        public Runner(List<Runner> runners)
        {
            _runners = runners;
        }

        public int ProcessId
        {
            get
            {
                if (_rogue == null) return 0;
                return _rogue.Id;
            }
        }

        public void Run() 
        { 
            while (true)
            {
                RunRogue();

                var logic = new Logic();
                Player player = null;

                var console = new ConsoleController(_rogue);

                while (!_rogue.HasExited)
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

        private void RunRogue()
        {
            const string processName = "rogue54";

            _rogue = null;

            var rogues = Process.GetProcessesByName(processName);
            foreach (var rogue in rogues)
            {
                if (!rogue.HasExited)
                {
                    if (!_runners.Any(r => r.ProcessId == rogue.Id))
                    {
                        // Claim it
                        _rogue = rogue;
                        return;
                    }
                }
            }

            var psi = new ProcessStartInfo
            {
                FileName = @"rogue54.exe",
                WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "rogue"),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            _rogue = Process.Start(psi);
            Thread.Sleep(1000);

            try
            {
                Debug.WriteLine($"Started new Rogue PID: {_rogue.Id}");
            }
            catch
            {
                Debug.WriteLine($"Error running rogue, try again");
                Thread.Sleep(1000);
                RunRogue();
            }
        }
    }
}
