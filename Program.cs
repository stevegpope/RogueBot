using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using static RogueBot.Native;

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
                Map previousMap = null;
                Map currentMap = null;
                Room startRoom = null;
                string previousMove = null;

                var console = ConsoleController.GetConsole(rogue);
                Player.InventoryCheck(console);

                while (!rogue.HasExited)
                {
                    try
                    {
                        currentMap = ConsoleController.WaitForScreenChange(console);
                        if (currentMap == null)
                        {
                            continue;
                        }

                        var move = C.Enter.ToString();

                        if (currentMap.HasString("More"))
                        {
                            move = C.Space.ToString();
                        }
                        else if (currentMap.HasString("call it"))
                        {
                            Debug.WriteLine("Naming randomly");
                            ConsoleController.SendKey("item " + Random.Shared.NextDouble());
                            ConsoleController.SendKey("{ENTER}");
                            Thread.Sleep(500);
                        }
                        else if (ConsoleController.Died(currentMap))
                        {
                            move = C.Enter.ToString();
                        }
                        else
                        {
                            if (startRoom == null)
                            {
                                startRoom = currentMap.Rooms.First();
                            }

                            if (previousMap != null && previousMove != C.DownStairs.ToString())
                            {
                                // Shift the map to align with the new viewport
                                currentMap.Shift(previousMap, startRoom.TopLeft);

                                if (logic.previousPosition == currentMap.Player)
                                {
                                    Debug.WriteLine($"Player position didn't change after move {previousMove}");
                                }
                            }

                            var player = new Player(currentMap);
                            if (player.Position != null)
                            {
                                if (player.Hungry())
                                {
                                    Player.Eat(console);
                                }
                                else if (currentMap.HasString(" found ") || currentMap.HasString("now have"))
                                {
                                    var foundStr = currentMap.GetString(" found ") ?? currentMap.GetString("now have");
                                    Player.Use(console, foundStr);
                                }

                                move = logic.ChooseMove(player, previousMap).ToString();
                            }

                            previousMap = currentMap;
                        }

                        previousMove = move;

                        if (move == C.Enter.ToString())
                        {
                            move = "{ENTER}";
                        }

                        // Bring Rogue window to foreground
                        Native.SetForegroundWindow(rogue.MainWindowHandle);
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
            const string processName = "rogue54"; // no .exe

            // 1. Try to find an existing process
            var existing = Process.GetProcessesByName(processName)
                                  .FirstOrDefault();

            if (existing != null && !existing.HasExited)
            {
                Console.WriteLine($"Reusing existing Rogue PID: {existing.Id}");
                return existing;
            }

            // 2. Otherwise start a new one
            var psi = new ProcessStartInfo
            {
                FileName = @"rogue54.exe",
                WorkingDirectory = Path.Combine(Environment.CurrentDirectory, "rogue"),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var rogue = Process.Start(psi);

            Console.WriteLine($"Started new Rogue PID: {rogue.Id}");

            Thread.Sleep(1000);
            return rogue;
        }
    }
}