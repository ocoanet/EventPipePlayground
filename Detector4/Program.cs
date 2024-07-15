// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Internal.Common.Utils;

var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator1\bin\Debug\net8.0\Allocator1.exe";

ProcessLauncher.Launcher.PrepareChildProcess(["--", targetPath]);

var builder = new DiagnosticsClientBuilder("EventPipePlayground", 10);

using var clientHolder = await builder.Build(CancellationToken.None, -1, "", true, true);

var process = Process.GetProcessById(clientHolder.EndpointInfo.ProcessId);

var eventPipeProvider = new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)GetClrRuntimeProviderKeywords());

using var startEventPipeSession = clientHolder.Client.StartEventPipeSession(eventPipeProvider, false);

clientHolder.Client.ResumeRuntime();

var eventSource = new EventPipeEventSource(startEventPipeSession.EventStream);

// eventSource.Clr.All += traceEvent =>
// {
//     Console.WriteLine(traceEvent);
// };

// eventSource.Clr.ClrStackWalk += traceEvent =>
// {
//     Console.WriteLine(traceEvent);
// };

eventSource.Clr.GCSampledObjectAllocation += traceEvent =>
{
    Console.WriteLine(traceEvent);
};

Console.WriteLine("Processing event source");

eventSource.Process();


process.WaitForExit();

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

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

