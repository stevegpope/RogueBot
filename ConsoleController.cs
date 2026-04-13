using System.Diagnostics;

namespace RogueBot
{
    public class ConsoleController
    {
        private static Process _rogue;

        public static nint GetConsole(Process rogue)
        {
            _rogue = rogue;

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

        public static Map WaitForTurnReady(IntPtr console)
        {
            Map map = null;
            var sw = Stopwatch.StartNew();

            var validStates = new[] { "More", "REST", "call it", "Level", "identify", "space" };


            while (true)
            {
                Thread.Sleep(5);

                var newMap = ReadMap(console);
                map = new Map(newMap);

                if (validStates.Any(s => map.HasString(s)))
                {
                    return map;
                }

                // Safety timeout (not primary logic)
                if (sw.ElapsedMilliseconds > 2000)
                    throw new Exception("Game did not reach a ready state");
            }
        }

        internal static List<string> WaitForText(nint console, params string[] search)
        {
            var lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            var stopwatch = Stopwatch.StartNew();
            while (!lines.Any(line => search.Any(s => line.Contains(s, StringComparison.OrdinalIgnoreCase))))
            {
                if (lines.Any(s => s.Contains("More")))
                {
                    ConsoleController.SendKey(C.Space);
                }

                Thread.Sleep(100);
                lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();

                if (stopwatch.Elapsed > TimeSpan.FromSeconds(2))
                    break;
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

            Debug.WriteLine($"Send key ({key})");

            if (!Native.WriteConsoleInput(hInput, inputs, (uint)inputs.Length, out var written))
            {
                throw new Exception("WriteConsoleInput failed");
            }

            Thread.Sleep(50);
        }
    }
}