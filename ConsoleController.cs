using System.Diagnostics;
using static System.Windows.Forms.LinkLabel;

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
                // 1. Get the REAL buffer dimensions (dwSize.X and dwSize.Y)
                if (!Native.GetConsoleScreenBufferInfo(hConsole, out var info))
                    throw new Exception("GetConsoleScreenBufferInfo failed");

                short actualWidth = info.dwSize.X;  // This is the key to fixing the staircase
                short actualHeight = info.dwSize.Y;

                // 2. Read the full buffer based on the actual size
                Native.CHAR_INFO[] buffer = new Native.CHAR_INFO[actualWidth * actualHeight];
                Native.SMALL_RECT region = new Native.SMALL_RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = (short)(actualWidth - 1),
                    Bottom = (short)(actualHeight - 1)
                };

                bool ok = Native.ReadConsoleOutput(
                    hConsole,
                    buffer,
                    new Native.COORD { X = actualWidth, Y = actualHeight },
                    new Native.COORD { X = 0, Y = 0 },
                    ref region);

                if (!ok) throw new Exception("ReadConsoleOutput failed");

                // 3. Map only the 80x27 area we care about
                const int TARGET_WIDTH = 80;
                const int TARGET_HEIGHT = 28;
                var map = new char[TARGET_HEIGHT][];

                for (int y = 0; y < TARGET_HEIGHT; y++)
                {
                    map[y] = new char[TARGET_WIDTH];
                    for (int x = 0; x < TARGET_WIDTH; x++)
                    {
                        // Align using actualWidth to skip the "empty" space at the end of rows
                        map[y][x] = buffer[y * actualWidth + x].UnicodeChar;
                    }
                }

                return map;
            });
        }

        public Map WaitForTurnReady()
        {
            Map map = null;
            var sw = Stopwatch.StartNew();

            var validStates = new[] { "more", "return", "killed", "call it", "level", "identify", "space", "worn" };

            while (true)
            {
                var newMap = ReadMap();
                map = new Map(newMap);

                if (validStates.Any(s => map.HasString(s)))
                {
                    return map;
                }

                // Safety timeout (not primary logic)
                if (sw.ElapsedMilliseconds > 2000)
                    throw new Exception($"{_pid} Game did not reach a ready state");

                Thread.Sleep(5);
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

                ushort vkCode;
                uint controlState = 0;
                char charToRead = key;

                if (key == C.Escape)
                {
                    vkCode = 0x1B;
                    charToRead = '\0'; // Send as a "Key Event" only, not a "Character"
                    controlState = 0;  // Force Shift OFF for Escape
                }
                else if (key == C.Enter)
                {
                    vkCode = 0x0D;     // VK_RETURN
                    charToRead = '\r'; // Enter character
                    controlState = 0;  // Force Shift OFF for Enter
                }
                else
                {
                    vkCode = (ushort)char.ToUpper(key);
                    // Only apply SHIFT if it's actually an uppercase letter
                    if (char.IsUpper(key))
                        controlState = 0x0080;
                }

                var records = new Native.INPUT_RECORD[2];

                // Key Down
                records[0].EventType = Native.KEY_EVENT;
                records[0].KeyEvent = new Native.KEY_EVENT_RECORD
                {
                    bKeyDown = true,
                    wRepeatCount = 1,
                    wVirtualKeyCode = vkCode,
                    UnicodeChar = charToRead,
                    dwControlKeyState = controlState
                };

                // Key Up
                records[1] = records[0];
                records[1].KeyEvent.bKeyDown = false;

                Native.WriteConsoleInput(hInput, records, 2, out _);
                return true;
            });

            Thread.Sleep(50);
        }
    }
}