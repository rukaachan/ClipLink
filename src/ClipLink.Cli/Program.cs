using System.ComponentModel;
using System.Diagnostics;
using ClipLink.Core;

return Run(args);

static int Run(string[] cliArgs)
{
    try
    {
        var command = ClipLinkCliCommandParser.Parse(cliArgs);
        return command switch
        {
            ClipLinkCliCommand.Start => StartWorker(),
            ClipLinkCliCommand.Stop => StopWorker(),
            ClipLinkCliCommand.Status => ReportStatus(),
            ClipLinkCliCommand.Restart => RestartWorker(),
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
        Console.WriteLine("ClipLink worker is already running.");
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

    Thread.Sleep(1200);
    if (!IsWorkerRunning())
    {
        Console.Error.WriteLine("ClipLink worker failed to stay running. Check %LOCALAPPDATA%\\ClipLink\\logs\\bridge.log (hotkey conflicts are common).");
        return 1;
    }

    Console.WriteLine("ClipLink worker started.");
    return 0;
}

static int StopWorker()
{
    var processes = GetWorkerProcesses();
    if (processes.Count == 0)
    {
        Console.WriteLine("ClipLink worker is not running.");
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

    Console.WriteLine("ClipLink worker stopped.");
    return 0;
}

static int ReportStatus()
{
    Console.WriteLine(IsWorkerRunning()
        ? "ClipLink worker is running."
        : "ClipLink worker is stopped.");
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
    var workers = new List<Process>();

    foreach (var process in Process.GetProcessesByName(WorkerProcessMetadata.ProcessName))
    {
        try
        {
            if (!process.HasExited)
            {
                workers.Add(process);
            }
            else
            {
                process.Dispose();
            }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            process.Dispose();
        }
    }

    return workers;
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: cliplink [start|stop|status|restart]");
}
