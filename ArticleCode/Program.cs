// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;

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
        // (see previous article).
        Console.WriteLine($"TypeID: {traceEvent.TypeID}");

        // Incorrect: throws InvalidOperationException because the trace
        // event is not issued by TraceLog.
        Console.WriteLine(traceEvent.CallStack());
    };

    eventSource.Process();
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
