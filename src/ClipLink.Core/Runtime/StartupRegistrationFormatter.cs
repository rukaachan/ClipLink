namespace ClipLink.Core
{
    public static class StartupRegistrationFormatter
    {
        public static string BuildRunCommand(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path is required.", nameof(executablePath));
            }

            return $"\"{executablePath}\"";
        }
    }
}
