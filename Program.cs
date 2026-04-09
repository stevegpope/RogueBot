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
                        var move = "{ENTER}";
                        var map = ReadMap(console);
                        var mapObj = new Map(map);

                        if (previousMap != null)
                        {
                            // Shift the map to align with the new viewport
                            mapObj.Shift(previousMap);
                        }

                        var hash = GetMapHash(map);
                        if (mapObj.Player != null)
                        {
                            var player = new Player(mapObj);
                            move = logic.ChooseMove(player, previousMap).ToString();
                        }

                        previousMap = mapObj;
                        previousMove = move;

                        if (move == C.Enter.ToString())
                        {
                            move = "{ENTER}";
                        }

                        // Bring Rogue window to foreground
                        Native.SetForegroundWindow(rogue.MainWindowHandle);

                        Debug.WriteLine(move);
                        SendKeys.SendWait(move);

                        currentMap = WaitForScreenChange(console, hash);
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

        private static List<string> ReadMap(IntPtr console)
        {
            const short WIDTH = 80;
            const short HEIGHT = 25;

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

            var map = new List<string>();

            for (int y = 0; y < HEIGHT; y++)
            {
                var line = new StringBuilder();
                for (int x = 0; x < WIDTH; x++)
                {
                    line.Append(buffer[y * WIDTH + x].UnicodeChar);
                }
                map.Add(line.ToString());
            }

            return map;
        }

        static Map WaitForScreenChange(IntPtr console, int previousHash)
        {
            const int timeoutMs = 500;
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Thread.Sleep(5);

                var newMap = ReadMap(console);
                if (GetMapHash(newMap) != previousHash)
                {
                    for(int y = 0; y < newMap.Count; y++)
                    {
                        Debug.WriteLine(newMap[y]);
                    }

                    Debug.WriteLine("new map hash: "  + GetMapHash(newMap));

                    if (newMap.Any(s => s.Contains(C.Player) || s.Contains("REST")))
                    {
                        return new Map(newMap); // screen updated
                    }
                }
            }

            return null;
        }

        static int GetMapHash(List<string> map)
        {
            // Generate a hash of the map to detect changes
            var result = new StringBuilder();
            foreach (var line in map)
            {
                result.Append(line);
            }

            return result.ToString().GetHashCode();
        }
    }
}