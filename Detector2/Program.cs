using System.Diagnostics;
using System.Runtime.InteropServices;
using Detector2;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

var traceFilePath = Path.GetTempFileName();
try
{
    ExecuteTracing(traceFilePath);
    ParseTraceFile(traceFilePath);
}
finally
{
    File.Delete(traceFilePath);
}

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

static void ExecuteTracing(string traceFilePath)
{
    var dotnetTracePath = GetDotnetTracePath();
    var clrRuntimeProvider = $"{ClrTraceEventParser.ProviderName}:{(ulong)GetClrRuntimeProviderKeywords()}:{(int)TraceEventLevel.Verbose}";
    var providers = $"{clrRuntimeProvider}";
    var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator1\bin\Debug\net8.0\Allocator1.exe";

    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            FileName = dotnetTracePath,
            ArgumentList =
            {
                "collect",
                "--providers",
                providers,
                // "--show-child-io",
                "--output",
                traceFilePath,
                "--",
                // targetPath,
                "dotnet",
                targetPath.Replace(".exe", ".dll"),
            },
            // RedirectStandardOutput = true,
            // RedirectStandardError = true,
        },
    };

    Console.WriteLine($"Starting tracing: {process.StartInfo.FileName} {string.Join(" ", process.StartInfo.ArgumentList)}");

    process.Start();
    process.WaitForExit();

    Console.WriteLine($"Tracing completed, TraceFileSize: {new FileInfo(traceFilePath).Length}, TraceFilePath: {traceFilePath}");
}

static ClrTraceEventParser.Keywords GetClrRuntimeProviderKeywords()
{
    return ClrTraceEventParser.Keywords.GC
           | ClrTraceEventParser.Keywords.Jit
           | ClrTraceEventParser.Keywords.JittedMethodILToNativeMap
           | ClrTraceEventParser.Keywords.Loader
           | ClrTraceEventParser.Keywords.Stack
           | ClrTraceEventParser.Keywords.Codesymbols
           | ClrTraceEventParser.Keywords.GCSampledObjectAllocationLow
           | ClrTraceEventParser.Keywords.GCSampledObjectAllocationHigh
           | ClrTraceEventParser.Keywords.GCHeapAndTypeNames
           | ClrTraceEventParser.Keywords.Type;
}

static string GetDotnetTracePath()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        return Path.Combine($"{Environment.GetEnvironmentVariable("HOME")}/.dotnet/tools", "dotnet-trace");

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return Path.Combine($"{Environment.GetEnvironmentVariable("USERPROFILE")}\\.dotnet\\tools", "dotnet-trace.exe");

    throw new PlatformNotSupportedException();
}

static void ParseTraceFile(string traceFilePath)
{
    if (!File.Exists(traceFilePath))
    {
        Console.WriteLine("Unable to find trace file");
        return;
    }

    Console.WriteLine("Starting trace file parsing");

    var eventSource = new EventPipeEventSource(traceFilePath);

    var stackParser = new EventPipeStackParser(eventSource);

    // eventSource.Clr.All += traceEvent =>
    // {
    //     Console.WriteLine(traceEvent);
    // };

    // eventSource.Clr.ClrStackWalk += traceEvent =>
    // {
    //     Console.WriteLine(traceEvent);
    // };

    Console.WriteLine("Processing event source");

    eventSource.Process();

    Console.WriteLine($"Trace file parsing completed");
}

