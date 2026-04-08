namespace ClipLink.Core
{
    public static class FileRetentionService
    {
        public static IReadOnlyList<string> GetExpiredFiles(
            string rootDirectory,
            DateTime now,
            TimeSpan retentionWindow)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retentionWindow, TimeSpan.Zero);

            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                return [];
            }

            var cutoff = now - retentionWindow;

            return Directory
                .GetFiles(rootDirectory)
                .Where(path => File.GetLastWriteTimeUtc(path) < cutoff.ToUniversalTime())
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
