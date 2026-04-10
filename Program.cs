using System.Diagnostics;
using System.Numerics;
using System.Text;
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
                                if (logic.pre)
                                // Shift the map to align with the new viewport
                                currentMap.Shift(previousMap, startRoom.TopLeft);
                            }

                            var player = new Player(currentMap);
                            if (player.Position != null)
                            {
                                if (player.Hungry())
                                {
                                    logic.Eat(console);
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

                        Debug.WriteLine(move);
                        SendKeys.SendWait(move);
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
            // --- Launch Rogue in its own console
            var psi = new ProcessStartInfo
            {
                FileName = @"C:\code\rogue\rogue54.exe",
                WorkingDirectory = @"C:\code\rogue",
                UseShellExecute = true,  // ensures new console
                CreateNoWindow = false
            };

            Process rogue = Process.Start(psi);
            Console.WriteLine($"Rogue PID: {rogue.Id}");

            // --- Give Rogue a moment to initialize
            Thread.Sleep(1000);

            Console.WriteLine("Bot started. Press Ctrl+C to stop.");
            return rogue;
        }
    }
}