namespace ClipLink.Core
{
    public static class SingleInstancePolicy
    {
        public static string BuildMutexName(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("Application name is required.", nameof(appName));
            }

            var sanitized = string.Concat(appName.Select(ch => char.IsWhiteSpace(ch) ? '_' : ch));
            return $@"Local\{sanitized}";
        }
    }
}
