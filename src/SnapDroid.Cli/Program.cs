using System.Diagnostics;
using SnapDroid.Core;

return Run(args);

static int Run(string[] cliArgs)
{
    try
    {
        var command = SnapDroidCliCommandParser.Parse(cliArgs);
        return command switch
        {
            SnapDroidCliCommand.Start => StartWorker(),
            SnapDroidCliCommand.Stop => StopWorker(),
            SnapDroidCliCommand.Status => ReportStatus(),
            SnapDroidCliCommand.Restart => RestartWorker(),
            _ => 1
        };
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        PrintUsage();
        return 1;
    }
}

static int StartWorker()
{
    if (IsWorkerRunning())
    {
        Console.WriteLine("SnapDroid worker is already running.");
        return 0;
    }

    var workerPath = WorkerProcessMetadata.ResolveExecutablePath(AppContext.BaseDirectory);
    if (!File.Exists(workerPath))
    {
        Console.Error.WriteLine($"Worker executable not found: {workerPath}");
        return 1;
    }

    Process.Start(new ProcessStartInfo
    {
        FileName = workerPath,
        UseShellExecute = true,
        WorkingDirectory = AppContext.BaseDirectory,
        WindowStyle = ProcessWindowStyle.Hidden
    });

    Console.WriteLine("SnapDroid worker started.");
    return 0;
}

static int StopWorker()
{
    var processes = GetWorkerProcesses();
    if (processes.Count == 0)
    {
        Console.WriteLine("SnapDroid worker is not running.");
        return 0;
    }

    foreach (var process in processes)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        finally
        {
            process.Dispose();
        }
    }

    Console.WriteLine("SnapDroid worker stopped.");
    return 0;
}

static int ReportStatus()
{
    Console.WriteLine(IsWorkerRunning()
        ? "SnapDroid worker is running."
        : "SnapDroid worker is stopped.");
    return 0;
}

static int RestartWorker()
{
    var stopExitCode = StopWorker();
    if (stopExitCode != 0)
    {
        return stopExitCode;
    }

    return StartWorker();
}

static bool IsWorkerRunning()
{
    using var process = Process.GetCurrentProcess();
    return GetWorkerProcesses()
        .Any(candidate => candidate.Id != process.Id);
}

static List<Process> GetWorkerProcesses()
{
    return Process
        .GetProcessesByName(WorkerProcessMetadata.ProcessName)
        .Where(process => !process.HasExited)
        .ToList();
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: snapdroid [start|stop|status|restart]");
}
