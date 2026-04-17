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

                InitializeState(console);

                while (!_rogue.HasExited)
                {
                    try
                    {
                        var currentMap = console.WaitForTurnReady();
                        if (currentMap == null)
                        {
                            continue;
                        }

                        if (player == null && currentMap.Player != null)
                        {
                            player = new Player(currentMap, console);
                            player.InventoryCheck();
                        }

                        char move;
                        if (currentMap.Player != null)
                        {
                            move = logic.ChooseMove(player, currentMap, console.Pid);
                        }
                        else
                        {
                            move = logic.ChooseMove(currentMap);
                            if (move == C.Unknown)
                            {
                                // Invalid state, try again
                                continue;
                            }
                        }

                        move = ValidateState(currentMap, move, console);

                        console.SendKey(move);
                    }
                    catch (Exception ex)
                    {
                        // Ignore errors if the window isn't ready yet
                        C.WriteLine(ProcessId, ex.Message);
                    }
                }
            }
        }

        private char ValidateState(Map map, char move, ConsoleController console)
        {
            var walkingMoves = new[] { C.Left, C.Right, C.Up, C.Down, C.UpLeft, C.UpRight, C.DownLeft, C.DownRight};
            if (!walkingMoves.Contains(move)) return move;

            ClearState(console);

            return move;
        }

        private void InitializeState(ConsoleController console)
        {
            var map = console.WaitForTurnReady();
            while (map == null)
            {
                map = console.WaitForTurnReady();
            }

            ClearState(console);
        }

        private void ClearState(ConsoleController console, int depth = 0)
        {
            var map = console.WaitForTurnReady();

            if (depth > Random.Shared.Next(5, 10))
            {
                C.WriteLine(ProcessId, "broke");
                return;
            }

            if (map.HasString("identify"))
            {
                var player = new Player(map, console);
                player.Identify();
                ClearState(console, depth + 1);
                return;
            }

            var spaceConditions = new[] { "in hand", "being worn", "space", "more" };
            var condition = spaceConditions.FirstOrDefault(c => map.HasString(c));
            if (condition != null)
            {
                console.SendKey(C.Space);
                map = console.WaitForTurnReady();
                ClearState(console, depth + 1);
                return;
            }

            var escapeConditions = new[] { "want to", "call", "quaff", "throw", "which direction" };
            condition = escapeConditions.FirstOrDefault(c => map.HasString(c));
            if (condition != null)
            {
                console.SendKey(C.Escape);
                console.SendKey(C.Enter);
                console.SendKey(C.Space);
                map = console.WaitForTurnReady();
                ClearState(console, depth + 1);
                return;
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
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            _rogue = Process.Start(psi);
            Thread.Sleep(1000);

            try
            {
                C.WriteLine(ProcessId, $"Started new Rogue PID: {_rogue.Id}");
            }
            catch
            {
                C.WriteLine(ProcessId, $"Error running rogue, try again");
                Thread.Sleep(1000);
                RunRogue();
            }
        }
    }
}
