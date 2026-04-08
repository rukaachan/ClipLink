namespace ClipLink.Core
{
    public static class ClipLinkCliCommandParser
    {
        public static ClipLinkCliCommand Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            if (args.Length == 0)
            {
                return ClipLinkCliCommand.Status;
            }

            return args[0].Trim().ToLowerInvariant() switch
            {
                "start" => ClipLinkCliCommand.Start,
                "stop" => ClipLinkCliCommand.Stop,
                "status" => ClipLinkCliCommand.Status,
                "restart" => ClipLinkCliCommand.Restart,
                _ => throw new ArgumentException($"Unsupported command '{args[0]}'.", nameof(args))
            };
        }
    }
}
