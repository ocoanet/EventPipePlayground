using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using Detector2;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator1\bin\Release\net8.0\Allocator1.exe";

var ipcAddress = CreateIpcAddress();

// 1: start a diagnostics server on a dedicated named IPC channel
await using var server = new ReversedDiagnosticsServer(ipcAddress);
server.Start();

// 2: start the target application with a specific environment variable
var childProcess = StartChildProcess(targetPath, environment: ("DOTNET_DiagnosticPorts", ipcAddress));

// 3: wait until the target application CLR connects to the IPC channel
var endpointInfo = server.Accept(TimeSpan.FromSeconds(10));

// 4: create a DiagnosticsClient using the internal, IPC-based constructor
var diagnosticsClient = new DiagnosticsClient(endpointInfo.Endpoint);

// 5: start the EventPipe session with the request providers
using var startEventPipeSession = diagnosticsClient.StartEventPipeSession(GetEventPipeProviders(), requestRundown: true);

// 6: resume the target application CLR
diagnosticsClient.ResumeRuntime();

var eventSource = new EventPipeEventSource(startEventPipeSession.EventStream);

var resolver = new EventPipeTypeResolver(eventSource);
var allocations = new List<(ulong typeId, EventPipeUnresolvedStack? callSack)>();

eventSource.Clr.GCSampledObjectAllocation += traceEvent =>
{
    var callStack = EventPipeUnresolvedStack.ReadFrom(traceEvent);
    allocations.Add((traceEvent.TypeID, callStack));
};

Console.WriteLine("Processing event source");

eventSource.Process();
childProcess.WaitForExit();

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
Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

static ClrTraceEventParser.Keywords GetClrRuntimeProviderKeywords()
{
    return ClrTraceEventParser.Keywords.GCSampledObjectAllocationLow
           | ClrTraceEventParser.Keywords.GCSampledObjectAllocationHigh
           | ClrTraceEventParser.Keywords.GCHeapAndTypeNames
           | ClrTraceEventParser.Keywords.Type;
}

static string CreateIpcAddress()
{
    var name = $"EventPipePlayground-{Process.GetCurrentProcess().Id}-{DateTime.Now:yyyyMMdd-HHmmss}.socket";
    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? name : Path.Combine(Path.GetTempPath(), name);
}

static Process StartChildProcess(string targetPath, params (string key, string value)[] environment)
{
    var process = new Process();
    process.StartInfo.FileName = targetPath;

    foreach (var (key, value) in environment)
    {
        process.StartInfo.Environment[key] = value;
    }

    process.Start();

    return process;
}

EventPipeProvider[] GetEventPipeProviders()
{
    return [new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)GetClrRuntimeProviderKeywords())];
}

static string FormatCallStack(EventPipeUnresolvedStack? unresolvedCallSack, EventPipeTypeResolver typeResolver)
{
    if (unresolvedCallSack == null)
        return "No callstack";

    var callStack = typeResolver.ResolveCallStack(unresolvedCallSack);
    return callStack.ToString();
}
