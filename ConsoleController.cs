using System.Diagnostics;

namespace RogueBot
{
    public class ConsoleController
    {
        private static int previousHash = 0;
        private static Process _rogue;

        public static nint GetConsole(Process rogue)
        {
            _rogue = rogue;

            // Bring Rogue window to foreground
            Native.SetForegroundWindow(rogue.MainWindowHandle);

            // Detach from current console FIRST
            Native.FreeConsole();

            // Attach to Rogue's console
            if (!Native.AttachConsole(rogue.Id))
            {
                throw new InvalidOperationException("Failed to attach to Rogue's console");
            }

            return Native.GetStdHandle(Native.STD_OUTPUT_HANDLE);
        }

        public static char[][] ReadMap(IntPtr console)
        {
            const short WIDTH = 80;
            const short HEIGHT = 26;

            Native.CHAR_INFO[] buffer = new Native.CHAR_INFO[WIDTH * HEIGHT];

            Native.SMALL_RECT region = new Native.SMALL_RECT
            {
                Left = 0,
                Top = 0,
                Right = WIDTH - 1,
                Bottom = HEIGHT - 1
            };

            bool ok = Native.ReadConsoleOutput(
                console,
                buffer,
                new Native.COORD { X = WIDTH, Y = HEIGHT },
                new Native.COORD { X = 0, Y = 0 },
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

        public static Map WaitForScreenChange(IntPtr console)
        {
            Map map = null;

            try
            {
                const int timeoutMs = 100;
                var sw = Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    Thread.Sleep(5);

                    var newMap = ReadMap(console);
                    map = new Map(newMap);
                    if (map.GetHashCode() == previousHash)
                    {
                        continue; // no change
                    }

                    if (Died(map))
                    {
                        return map;
                    }

                    var player = new Player(map);
                    if (player.Position == null)
                    {
                        continue;
                    }

                    if (map.Player != null)
                    {
                        for (int y = 0; y < newMap.Length; y++)
                        {
                            //Debug.WriteLine(new string(newMap[y]));
                        }

                        return new Map(newMap); // screen updated
                    }
                }

                return map;
            }
            finally
            {
                if (map != null)
                {
                    previousHash = map.GetHashCode();
                }
            }
        }

        public static bool Died(Map map)
        {
            return map.HasString("REST") || map.HasString("Score");
        }

        internal static List<string> WaitForText(nint console, params string[] search)
        {
            var lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            while (!lines.Any(line => search.Any(s => line.Contains(s, StringComparison.OrdinalIgnoreCase))))
            {
                if (lines.Any(s => s.Contains("More")))
                {
                    ConsoleController.SendKey(C.Space);
                }

                Thread.Sleep(100);
                lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            }

            return lines;
        }

        internal static void SendKey(string keys)
        {
            foreach (var key in keys)
            {
                SendKey(key);
            }
        }

        internal static void SendKey(char key)
        {
            Native.SetForegroundWindow(_rogue.MainWindowHandle);

            var hInput = Native.GetStdHandle(Native.STD_INPUT_HANDLE);

            var inputs = new Native.INPUT_RECORD[2];

            // Key down
            inputs[0] = new Native.INPUT_RECORD
            {
                EventType = Native.KEY_EVENT,
                KeyEvent = new Native.KEY_EVENT_RECORD
                {
                    bKeyDown = true,
                    wRepeatCount = 1,
                    wVirtualKeyCode = (ushort)char.ToUpper(key),
                    wVirtualScanCode = 0,
                    UnicodeChar = key,
                    dwControlKeyState = 0
                }
            };

            // Key up
            inputs[1] = new Native.INPUT_RECORD
            {
                EventType = Native.KEY_EVENT,
                KeyEvent = new Native.KEY_EVENT_RECORD
                {
                    bKeyDown = false,
                    wRepeatCount = 1,
                    wVirtualKeyCode = (ushort)char.ToUpper(key),
                    wVirtualScanCode = 0,
                    UnicodeChar = key,
                    dwControlKeyState = 0
                }
            };

            if (!Native.WriteConsoleInput(hInput, inputs, (uint)inputs.Length, out var written))
            {
                throw new Exception("WriteConsoleInput failed");
            }

            Thread.Sleep(10);
        }
    }
}