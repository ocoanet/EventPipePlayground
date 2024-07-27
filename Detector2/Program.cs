using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Detector2;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

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
    var clrRuntimeProvider = $"{ClrTraceEventParser.ProviderName}:0x{(ulong)GetClrRuntimeProviderKeywords():X}:{(int)TraceEventLevel.Verbose}";
    var providers = $"{clrRuntimeProvider}";
    var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator1\bin\Release\net8.0\Allocator1.exe";

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
    return ClrTraceEventParser.Keywords.GCSampledObjectAllocationLow
           | ClrTraceEventParser.Keywords.GCSampledObjectAllocationHigh
           | ClrTraceEventParser.Keywords.GCHeapAndTypeNames
           | ClrTraceEventParser.Keywords.Type
        ;
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

    var resolver = new EventPipeTypeResolver(eventSource);

    var allocations = new List<(ulong typeId, EventPipeStack? callSack)>();

    eventSource.Clr.GCSampledObjectAllocation += traceEvent =>
    {
        var callStack = EventPipeStack.ReadFrom(traceEvent);
        allocations.Add((traceEvent.TypeID, callStack));
    };

    Console.WriteLine("Processing event source");

    eventSource.Process();

    var allocationCounts = allocations.Select(x => (typeName: resolver.GetTypeName(x.typeId), callStack: FormatCallStack(x.callSack, resolver)))
                                      .Where(x => !x.callStack.Contains("ManagedStartup"))
                                      .GroupBy(x => x)
                                      .Select(g => (allocation: g.Key, count: g.Count()))
                                      .Where(x => x.count != 1)
                                      .OrderByDescending(x => x.count);

    foreach (var (allocation, count) in allocationCounts)
    {
        Console.WriteLine("///////////////////////////");
        Console.WriteLine($"Type: {allocation.typeName}");
        Console.WriteLine($"Count: {count}");
        Console.WriteLine(allocation.callStack);
    }

    Console.WriteLine($"Trace file parsing completed");
}

static string FormatCallStack(EventPipeStack? unresolvedCallSack, EventPipeTypeResolver typeResolver)
{
    if (unresolvedCallSack == null)
        return "No callstack";

    var stringBuilder = new StringBuilder();
    var callStack = typeResolver.ResolveCallStack(unresolvedCallSack);
    foreach (var address in callStack.Addresses)
    {
        stringBuilder.AppendLine($"     {address}");
    }

    return stringBuilder.ToString();
}
