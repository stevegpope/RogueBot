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

                // Bring Rogue window to foreground
                Native.SetForegroundWindow(rogue.MainWindowHandle);

                // Detach from current console FIRST
                Native.FreeConsole();

                // Attach to Rogue's console
                if (!Native.AttachConsole(rogue.Id))
                {
                    Console.WriteLine("AttachConsole failed!");
                    return;
                }

                IntPtr console = Native.GetStdHandle(Native.STD_OUTPUT_HANDLE);

                while (!rogue.HasExited)
                {
                    try
                    {
                        currentMap = WaitForScreenChange(console);
                        if (currentMap == null)
                        {
                            continue;
                        }


                        var move = C.Enter.ToString();

                        if (currentMap.HasString("More"))
                        {
                            move = C.Space.ToString();
                        }
                        else if (currentMap.HasString("REST"))
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
                            }

                            var player = new Player(currentMap);
                            if (player.Position != null)
                            {
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

        private static char[][] ReadMap(IntPtr console)
        {
            const short WIDTH = 80;
            const short HEIGHT = 26;

            CHAR_INFO[] buffer = new CHAR_INFO[WIDTH * HEIGHT];

            SMALL_RECT region = new SMALL_RECT
            {
                Left = 0,
                Top = 0,
                Right = WIDTH - 1,
                Bottom = HEIGHT - 1
            };

            bool ok = ReadConsoleOutput(
                console,
                buffer,
                new COORD { X = WIDTH, Y = HEIGHT },
                new COORD { X = 0, Y = 0 },
                ref region);

            if (!ok)
                throw new Exception("ReadConsoleOutput failed");

            var map = new char[HEIGHT][];

            for (int y = 0; y < HEIGHT; y++)
            {
                map[y] = new char[WIDTH];
                for (int x = 0; x < WIDTH; x++)
                {
                    map[y][x] = buffer[y * WIDTH + x].UnicodeChar;
                }
            }

            return map;
        }

        static Map WaitForScreenChange(IntPtr console)
        {
            const int timeoutMs = 1000;
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Thread.Sleep(5);

                var newMap = ReadMap(console);
                var map = new Map(newMap);

                if (map.HasString("REST"))
                {
                    return map;
                }

                if (map.Rooms.Count > 0)
                {
                    for (int y = 0; y < newMap.Length; y++)
                    {
                        Debug.WriteLine(new string(newMap[y]));
                    }

                    var player = new Player(map);
                    if (player.Position == null)
                    {
                        continue;
                    }

                    if (map.Player != null)
                    {
                        return new Map(newMap); // screen updated
                    }
                }
            }

            return null;
        }
    }
}