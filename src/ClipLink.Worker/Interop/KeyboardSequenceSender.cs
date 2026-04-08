namespace ClipLink.Worker
{
    internal readonly record struct KeyTransition(byte VirtualKey, bool IsKeyUp);

    internal static class KeyboardSequenceSender
    {
        public static void SendPaste(string sequence)
        {
            var transitions = BuildKeyTransitions(sequence);
            var inputs = transitions
                .Select(transition => NativeMethods.CreateKeyboardInput(transition.VirtualKey, transition.IsKeyUp))
                .ToArray();

            var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.InputSize);
            if (sent != inputs.Length)
            {
                SendChordFallback(transitions);
            }
        }

        internal static IReadOnlyList<KeyTransition> BuildKeyTransitions(string sequence)
        {
            byte[] keys = sequence switch
            {
                "^+v" => [NativeMethods.VkControl, NativeMethods.VkShift, NativeMethods.VkV],
                "+{INSERT}" => [NativeMethods.VkShift, NativeMethods.VkInsert],
                "^v" => [NativeMethods.VkControl, NativeMethods.VkV],
                _ => throw new InvalidOperationException($"Unsupported paste sequence '{sequence}'.")
            };

            var transitions = new List<KeyTransition>(keys.Length * 2);
            foreach (var key in keys)
            {
                transitions.Add(new KeyTransition(key, false));
            }

            for (var i = keys.Length - 1; i >= 0; i--)
            {
                transitions.Add(new KeyTransition(keys[i], true));
            }

            return transitions;
        }

        private static void SendChordFallback(IReadOnlyList<KeyTransition> transitions)
        {
            foreach (var transition in transitions)
            {
                NativeMethods.keybd_event(
                    transition.VirtualKey,
                    0,
                    transition.IsKeyUp ? NativeMethods.KeyeventfKeyup : 0,
                    UIntPtr.Zero);
            }
        }
    }
}

