namespace SnapDroid.Core
{
    public sealed class BridgeConfiguration
    {
        public string ImageRootDirectory { get; init; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "SnapDroid");

        public string PromptTemplate { get; init; } = "analyze this image: {path}";

        public static string ExpandPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? path
                : Environment.ExpandEnvironmentVariables(path);
        }

        public string ResolveStorageDirectory(DateTime now)
        {
            return ExpandPath(ImageRootDirectory);
        }

        public string BuildPrompt(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("Image path is required.", nameof(imagePath));
            }

            if (!PromptTemplate.Contains("{path}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Prompt template must contain the {path} placeholder.");
            }

            return PromptTemplate.Replace("{path}", imagePath, StringComparison.Ordinal);
        }
    }
}
