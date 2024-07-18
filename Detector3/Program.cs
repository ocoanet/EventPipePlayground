using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using ClrDebug;using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;

var targetPath = @"C:\Dev\Repos\EventPipePlayground\Allocator1\bin\Debug\net8.0\Allocator1.exe";

var dbgshim = new DbgShim(NativeLibrary.Load("dbgshim", typeof(Program).Assembly, null));

var launchResult = dbgshim.CreateProcessForLaunch(targetPath, bSuspendProcess: true);

var diagnosticsClient = new DiagnosticsClient(launchResult.ProcessId);

// If StartEventPipeSession is invoked before ResumeProcess, StartEventPipeSession hangs.
// If StartEventPipeSession is invoked after ResumeProcess, it is too late to enable provider allocation keywords.

dbgshim.ResumeProcess(launchResult.ResumeHandle);

using var startEventPipeSession = diagnosticsClient.StartEventPipeSession(new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)GetClrRuntimeProviderKeywords()), false);

var eventSource = new EventPipeEventSource(startEventPipeSession.EventStream);

eventSource.Clr.GCSampledObjectAllocation += traceEvent =>
{
    Console.WriteLine(traceEvent);
};

Console.WriteLine("Processing event source");

eventSource.Process();

dbgshim.CloseResumeHandle(launchResult.ResumeHandle);

var process = Process.GetProcessById(launchResult.ProcessId);
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

