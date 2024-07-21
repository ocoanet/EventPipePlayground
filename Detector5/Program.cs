using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Allocator2;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
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
    var clrRuntimeProvider = $"{ClrTraceEventParser.ProviderName}:0x{(long)GetClrRuntimeProviderKeywords():X}:{(int)TraceEventLevel.Verbose}";
    var replayProvider = ReplayEventSource.ProviderName;
    var providers = $"{clrRuntimeProvider},{replayProvider}";
    var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator2\bin\Release\net8.0\Allocator2.exe";

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
                targetPath,
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

    var traceLogOptions = new TraceLogOptions
    {
        ConversionLog = TextWriter.Null,
        AlwaysResolveSymbols = true,
        LocalSymbolsOnly = true,
        ShouldResolveSymbols = _ => true,
        ContinueOnError = true,
    };

    var etlxFilePath = TraceLog.CreateFromEventPipeDataFile(traceFilePath, options: traceLogOptions);
    try
    {
        using var traceLog = new TraceLog(etlxFilePath);

        var typeNames = new Dictionary<ulong, string>();
        var currentEventStoreEvent = (EventStoreEvent?)null;

        foreach (var traceEvent in traceLog.Events)
        {
            if (traceEvent.ProviderName == ReplayEventSource.ProviderName)
            {
                if (traceEvent.ID == (TraceEventID)ReplayEventSource.EventStoreEventProcessingStartId)
                {
                    // It is also possible to use a TraceEventParser to create custom event types.
                    currentEventStoreEvent = new EventStoreEvent(
                        (long)traceEvent.PayloadValue(0),
                        (long)traceEvent.PayloadValue(1)
                    );
                }
                else if (traceEvent.ID == (TraceEventID)ReplayEventSource.EventStoreEventProcessingStopId)
                {
                    currentEventStoreEvent = null;
                }
            }

            // Use currentEventStoreEvent to ignore or add context to allocations
            // ...

            if (traceEvent is GCBulkTypeTraceData bulkTypeTraceData)
                LoadTypeNames(typeNames, bulkTypeTraceData);

            if (traceEvent is GCSampledObjectAllocationTraceData allocationTraceData && currentEventStoreEvent != null)
                ProcessAllocation(allocationTraceData, currentEventStoreEvent.Value, typeNames);
        }
    }
    finally
    {
        File.Delete(etlxFilePath);
    }

    Console.WriteLine($"Trace file parsing completed");
}

static void LoadTypeNames(Dictionary<ulong,string> typeNames, GCBulkTypeTraceData traceEvent)
{
    for (var index = 0; index < traceEvent.Count; index++)
    {
        var typeData = traceEvent.Values(index);
        typeNames[typeData.TypeID] = typeData.TypeName;
    }
}

static void ProcessAllocation(GCSampledObjectAllocationTraceData traceEvent, EventStoreEvent eventStoreEvent, Dictionary<ulong, string> typeNames)
{
    Console.WriteLine("///////////////////////////");
    Console.WriteLine($"Type: {typeNames[traceEvent.TypeID]}");
    Console.WriteLine($"EventStoreEventId: {eventStoreEvent.Id}");
    Console.WriteLine(FormatCallStack(traceEvent.CallStack()));
}

static string FormatCallStack(TraceCallStack? callStack)
{
    if (callStack == null)
        return "No callstack";

    var stringBuilder = new StringBuilder();
    while (callStack != null)
    {
        var methodName = callStack.CodeAddress.FullMethodName;
        if (!string.IsNullOrEmpty(methodName))
            stringBuilder.AppendLine($"     {methodName}");
        else
            stringBuilder.AppendLine($"     0x{callStack.CodeAddress.Address:X}");

        callStack = callStack.Caller;
    }

    return stringBuilder.ToString();
}

readonly record struct EventStoreEvent(long Id, long Timestamp);
