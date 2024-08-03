using System.Diagnostics;
using System.Diagnostics.Tracing;
using Detector2;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Internal.Common.Utils;

var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator1\bin\Release\net8.0\Allocator1.exe";

ProcessLauncher.Launcher.PrepareChildProcess(["--", targetPath]);

var builder = new DiagnosticsClientBuilder("EventPipePlayground", 10);

using var clientHolder = await builder.Build(CancellationToken.None, -1, "", true, true);

var process = Process.GetProcessById(clientHolder.EndpointInfo.ProcessId);

var eventPipeProvider = new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)GetClrRuntimeProviderKeywords());

using var startEventPipeSession = clientHolder.Client.StartEventPipeSession(eventPipeProvider, requestRundown: true);

clientHolder.Client.ResumeRuntime();

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

process.WaitForExit();

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

static ClrTraceEventParser.Keywords GetClrRuntimeProviderKeywords()
{
    return ClrTraceEventParser.Keywords.GCSampledObjectAllocationLow
           | ClrTraceEventParser.Keywords.GCSampledObjectAllocationHigh
           | ClrTraceEventParser.Keywords.GCHeapAndTypeNames
           | ClrTraceEventParser.Keywords.Type;
}

static string FormatCallStack(EventPipeUnresolvedStack? unresolvedCallSack, EventPipeTypeResolver typeResolver)
{
    if (unresolvedCallSack == null)
        return "No callstack";

    var callStack = typeResolver.ResolveCallStack(unresolvedCallSack);
    return callStack.ToString();
}
