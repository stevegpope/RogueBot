using System.Diagnostics;
using System.Numerics;
using System.Text;
using static RogueBot.Native;

namespace RogueBot
{
    public class ConsoleController
    {
        public static nint GetConsole(Process rogue)
        {
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

        public static Map WaitForScreenChange(IntPtr console)
        {
            const int timeoutMs = 1000;
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Thread.Sleep(5);

                var newMap = ReadMap(console);
                var map = new Map(newMap);

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
                        Debug.WriteLine(new string(newMap[y]));
                    }

                    return new Map(newMap); // screen updated
                }
            }

            return null;
        }

        public static bool Died(Map map)
        {
            return map.HasString("REST") || map.HasString("Score");
        }

        internal static List<string> WaitForText(nint console, string search)
        {
            var lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            while (!lines.Any(line => line.Contains(search)))
            {
                Thread.Sleep(100);
                lines = ConsoleController.ReadMap(console).Select(line => new string(line)).ToList();
            }

            return lines;
        }
    }
}