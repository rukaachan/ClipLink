using ClipLink.Core;

namespace ClipLink.Worker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClipLink");
            var logger = new FileLogger(Path.Combine(appDataRoot, "logs"));
            using var mutex = new Mutex(
                initiallyOwned: true,
                SingleInstancePolicy.BuildMutexName("ClipLink"),
                out var isFirstInstance);

            try
            {
                if (!isFirstInstance)
                {
                    logger.Info("Worker launch skipped because another instance is already running.");
                    return;
                }

                logger.Info("Worker starting.");
                ApplicationConfiguration.Initialize();
                Application.Run(new BackgroundWorkerContext());
                logger.Info("Worker message loop exited.");
            }
            catch (Exception ex)
            {
                logger.Error("Worker startup failed.", ex);
                throw;
            }
        }
    }
}

