namespace SnapDroid.Core
{
    public static class WorkerProcessMetadata
    {
        public const string ProcessName = "SnapDroid.Worker";
        public const string ExecutableFileName = ProcessName + ".exe";

        public static string ResolveExecutablePath(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException("Base directory is required.", nameof(baseDirectory));
            }

            return Path.Combine(baseDirectory, ExecutableFileName);
        }
    }
}
