namespace SnapDroid.Core
{
    public static class SnapDroidCliCommandParser
    {
        public static SnapDroidCliCommand Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            if (args.Length == 0)
            {
                return SnapDroidCliCommand.Status;
            }

            return args[0].Trim().ToLowerInvariant() switch
            {
                "start" => SnapDroidCliCommand.Start,
                "stop" => SnapDroidCliCommand.Stop,
                "status" => SnapDroidCliCommand.Status,
                "restart" => SnapDroidCliCommand.Restart,
                _ => throw new ArgumentException($"Unsupported command '{args[0]}'.", nameof(args))
            };
        }
    }
}
