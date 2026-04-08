using System.Runtime.InteropServices;

namespace ClipLink.Worker
{
    internal static class NativeMethods
    {
        public const int WmHotKey = 0x0312;
        public const byte VkShift = 0x10;
        public const byte VkControl = 0x11;
        public const byte VkV = 0x56;
        public const byte VkInsert = 0x2D;
        public const uint KeyeventfKeyup = 0x0002;
        public const uint InputKeyboard = 1;
        public const uint KeyeventfScancode = 0x0008;
        public static readonly int InputSize = Marshal.SizeOf<INPUT>();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

        public static INPUT CreateKeyboardInput(byte virtualKey, bool keyUp)
        {
            return new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = keyUp ? KeyeventfKeyup : 0,
                        dwExtraInfo = IntPtr.Zero,
                        time = 0
                    }
                }
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}

