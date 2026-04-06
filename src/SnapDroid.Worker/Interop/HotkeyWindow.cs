namespace SnapDroid.Worker
{
    internal sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        public const int PasteHotkeyId = 1001;
        public const int CopyHotkeyId = 1002;

        public event Action<int>? HotkeyPressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        public bool RegisterHotkey(int id, HotkeyBinding binding)
        {
            return NativeMethods.RegisterHotKey(Handle, id, (uint)binding.Modifiers, (uint)binding.Key);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WmHotKey)
            {
                HotkeyPressed?.Invoke(m.WParam.ToInt32());
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            NativeMethods.UnregisterHotKey(Handle, PasteHotkeyId);
            NativeMethods.UnregisterHotKey(Handle, CopyHotkeyId);
            DestroyHandle();
        }
    }
}

