// See https://aka.ms/new-console-template for more information

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;

TypeProxyExample();

Console.WriteLine("Press Enter to exit...");
Console.ReadLine();

static void EventPipeEventSourceSample(string netTraceFilePath)
{
    using var eventSource = new EventPipeEventSource(netTraceFilePath);

    eventSource.Clr.GCSampledObjectAllocation += traceEvent =>
    {
        Console.WriteLine("Allocation Found!");

        // The name can be resolved using the GCBulkTypeTraceData events
        // (see previous post).
        Console.WriteLine($"TypeID: {traceEvent.TypeID}");

        // Incorrect!
        Console.WriteLine(traceEvent.CallStack());
    };

    eventSource.Process();
}

static void DiagnosticsClientSample(int processId)
{
    var client = new DiagnosticsClient(processId);

    var provider = new EventPipeProvider(
        name: ClrTraceEventParser.ProviderName,
        eventLevel: EventLevel.Informational,
        keywords: GetClrProviderKeywords()
    );

    using var session = client.StartEventPipeSession(provider, false);
    using var source = new EventPipeEventSource(session.EventStream);

    source.Clr.All += traceEvent => Console.WriteLine(traceEvent.EventName);
    source.Process();
}

static long GetClrProviderKeywords()
{
    return (long)ClrTraceEventParser.Keywords.Default;
}

static void TypeProxyExample()
{
    var list = new List<int> { 1, 2, 3 };

    var version = GetVersion(list);

    Console.WriteLine($"Version: {version}");
}

static int GetVersion<T>(List<T> list)
{
    var proxy = Unsafe.As<List<T>, ListProxy<T>>(ref list);

    return proxy._version;
}

class ListProxy<T>
{
    internal T[] _items;
    internal int _size;
    internal int _version;
}
