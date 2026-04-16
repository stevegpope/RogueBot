using System.Diagnostics;

namespace RogueBot
{
    public class ConsoleController
    {
        private static readonly SemaphoreSlim _apiLock = new SemaphoreSlim(1, 1);

        private int _pid;

        public ConsoleController(Process rogue)
        {
            _pid = rogue.Id;
        }

        private T ExecuteConsoleAction<T>(Func<IntPtr, T> action)
        {
            _apiLock.Wait();

            try
            {
                Native.FreeConsole(); // Must be free before we can attach
                if (!Native.AttachConsole(_pid))
                    throw new Exception($"Failed to attach to PID {_pid}");

                IntPtr hConsole = Native.GetStdHandle(Native.STD_OUTPUT_HANDLE);
                return action(hConsole);
            }
            finally
            {
                Native.FreeConsole();
                _apiLock.Release();
            }
        }

        public char[][] ReadMap()
        {
            return ExecuteConsoleAction(hConsole =>
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
                    hConsole,
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
            });
        }

        public Map WaitForTurnReady()
        {
            Map map = null;
            var sw = Stopwatch.StartNew();

            var validStates = new[] { "More", "REST", "call it", "Level", "identify", "space" };

            while (true)
            {
                Thread.Sleep(5);

                var newMap = ReadMap();
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

        internal List<string> WaitForText(params string[] search)
        {
            var lines = ReadMap().Select(line => new string(line)).ToList();
            var stopwatch = Stopwatch.StartNew();
            while (!lines.Any(line => search.Any(s => line.Contains(s, StringComparison.OrdinalIgnoreCase))))
            {
                if (lines.Any(s => s.Contains("More")))
                {
                    SendKey(C.Space);
                }

                Thread.Sleep(100);
                lines = ReadMap().Select(line => new string(line)).ToList();

                if (stopwatch.Elapsed > TimeSpan.FromSeconds(2))
                    break;
            }

            return lines;
        }

        internal void SendKey(string keys)
        {
            foreach (var key in keys)
            {
                SendKey(key);
            }
        }

        internal void SendKey(char key)
        {
            ExecuteConsoleAction(hConsole => {
                IntPtr hInput = Native.GetStdHandle(Native.STD_INPUT_HANDLE);
                bool isUpper = char.IsUpper(key);
                ushort vkCode = (ushort)char.ToUpper(key);

                var records = new Native.INPUT_RECORD[2];
                records[0].EventType = Native.KEY_EVENT;
                records[0].KeyEvent = new Native.KEY_EVENT_RECORD
                {
                    bKeyDown = true,
                    wRepeatCount = 1,
                    wVirtualKeyCode = vkCode,
                    UnicodeChar = key,
                    dwControlKeyState = (uint)(isUpper ? 0x0080 : 0)
                };
                records[1] = records[0];
                records[1].KeyEvent.bKeyDown = false;

                Debug.WriteLine($"Send key ({key})");
                Native.WriteConsoleInput(hInput, records, 2, out _);
                return true;
            });

            Thread.Sleep(50);
        }
    }
}