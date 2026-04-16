using System.Runtime.InteropServices;

namespace RogueBot
{
    public class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CHAR_INFO
        {
            public char UnicodeChar;
            public short Attributes;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleOutput(
            IntPtr hConsoleOutput,
            [Out] CHAR_INFO[] lpBuffer,
            COORD dwBufferSize,
            COORD dwBufferCoord,
            ref SMALL_RECT lpReadRegion);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadConsoleOutputCharacter(
            IntPtr hConsoleOutput,
            System.Text.StringBuilder lpCharacter,
            uint nLength,
            COORD dwReadCoord,
            out uint lpNumberOfCharsRead);

        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();


        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_INPUT_HANDLE = -10;
        public const short KEY_EVENT = 0x0001;
        public const int ATTACH_PARENT_PROCESS = -1;

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public ushort wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleInput(
            IntPtr hConsoleInput,
            INPUT_RECORD[] lpBuffer,
            uint nLength,
            out uint lpNumberOfEventsWritten);

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public short EventType;

            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_EVENT_RECORD
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool bKeyDown;

            public ushort wRepeatCount;
            public ushort wVirtualKeyCode;
            public ushort wVirtualScanCode;
            public char UnicodeChar;
            public uint dwControlKeyState;
        }
    }
}
