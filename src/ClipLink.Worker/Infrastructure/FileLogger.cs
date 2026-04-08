namespace ClipLink.Worker
{
    internal sealed class FileLogger
    {
        private readonly string logFilePath;

        public FileLogger(string logDirectory)
        {
            Directory.CreateDirectory(logDirectory);
            logFilePath = Path.Combine(logDirectory, "bridge.log");
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        public void Error(string message, Exception exception)
        {
            Write("ERROR", $"{message} {exception}");
        }

        private void Write(string level, string message)
        {
            File.AppendAllText(
                logFilePath,
                $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
        }
    }
}

