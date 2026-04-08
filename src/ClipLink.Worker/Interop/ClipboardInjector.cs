using ClipLink.Core;

namespace ClipLink.Worker
{
    internal static class ClipboardInjector
    {
        public static void PastePrompt(
            string prompt,
            IntPtr targetWindow,
            string? targetProcessName,
            PasteInjectionSettings settings,
            FileLogger logger)
        {
            IDataObject? originalClipboard = null;

            try
            {
                originalClipboard = Clipboard.GetDataObject();
                Clipboard.SetText(prompt);

                if (targetWindow != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(targetWindow);
                    WaitForForegroundWindow(targetWindow, TimeSpan.FromMilliseconds(300));
                }

                Thread.Sleep(75);
                var sequence = settings.ResolveSequence(targetProcessName);
                var restoreDelayMilliseconds = settings.ResolveRestoreDelayMilliseconds(targetProcessName);
                logger.Info($"Using paste sequence '{sequence}' for process '{targetProcessName ?? "unknown"}'.");
                KeyboardSequenceSender.SendPaste(sequence);
                Thread.Sleep(restoreDelayMilliseconds);
            }
            finally
            {
                if (settings.RestoreClipboardAfterPaste && originalClipboard is not null)
                {
                    try
                    {
                        Clipboard.SetDataObject(originalClipboard, true);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Failed to restore original clipboard contents.", ex);
                    }
                }
            }
        }

        private static void WaitForForegroundWindow(IntPtr expectedWindow, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (NativeMethods.GetForegroundWindow() == expectedWindow)
                {
                    return;
                }

                Thread.Sleep(25);
            }
        }
    }
}

