using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Detector2;

public unsafe class EventPipeStackParser
{
    private readonly List<ParserMethod> _methods = new();
    private readonly ParserMethod _lookupParserMethod = new();
    private readonly Dictionary<ulong, string> _typeNames = new();

    public EventPipeStackParser(EventPipeEventSource eventSource)
    {
        // TODO: Fix method lookup
        eventSource.Clr.MethodLoadVerbose += OnMethodLoadVerbose;
        eventSource.Clr.MethodDCStartVerboseV2 += OnMethodLoadVerbose;
        eventSource.Clr.TypeBulkType += OnTypeBulkType;
    }

    public string? GetTypeName(ulong typeId)
    {
        return _typeNames.GetValueOrDefault(typeId);
    }

    private void OnTypeBulkType(GCBulkTypeTraceData traceData)
    {
        for (var index = 0; index < traceData.Count; index++)
        {
            var typeData = traceData.Values(index);
            _typeNames[typeData.TypeID] = typeData.TypeName;
        }
    }

    private void OnMethodLoadVerbose(MethodLoadUnloadVerboseTraceData traceData)
    {
        if (!traceData.IsJitted)
            return;

        var index = SearchMethodIndex(traceData.MethodStartAddress);
        if (index >= 0)
            return;

        var insertIndex = ~index;
        _methods.Insert(insertIndex, new ParserMethod
        {
            FullName = GetFullName(traceData),
            StartAddress = traceData.MethodStartAddress,
            Length = (uint)traceData.MethodSize,
        });
    }

    private int SearchMethodIndex(ulong address)
    {
        _lookupParserMethod.StartAddress = address;

        return _methods.BinarySearch(_lookupParserMethod, ParserMethodAddressComparer.Instance);
    }

    private string? GetMethodFullName(ulong address)
    {
        var index = SearchMethodIndex(address);
        if (index >= 0)
            return _methods[index].FullName;

        var previousIndex = (~index) - 1;
        if (previousIndex >= 0 && _methods[previousIndex].ContainsAddress(address))
            return _methods[previousIndex].FullName;

        return null;
    }

    public EventPipeCallStack? GetCallStack(TraceEvent traceEvent)
    {
        var traceEventProxy = Unsafe.As<TraceEvent, TraceEventProxy>(ref traceEvent);
        if (traceEventProxy.eventRecord == null)
            return null;

        var extendedData = traceEventProxy.eventRecord->ExtendedData;
        var extendedDataCount = traceEventProxy.eventRecord->ExtendedDataCount;

        for (var i = 0; i < extendedDataCount; i++)
        {
            if (extendedData[i].ExtType != TraceEventNativeMethods.EVENT_HEADER_EXT_TYPE_STACK_TRACE64)
                continue;

            var pointerSize = 8;
            var stackRecord = (TraceEventNativeMethods.EVENT_EXTENDED_ITEM_STACK_TRACE64*)extendedData[i].DataPtr;

            var addresses = &stackRecord->Address[0];
            var addressCount = (extendedData[i].DataSize - sizeof(ulong)) / pointerSize;

            var callStackAddresses = new List<EventPipeCallStackAddress>();
            for (var a = addressCount - 1; a >= 0; a--)
            {
                var address = addresses[a];
                var methodFullName = GetMethodFullName(address);
                callStackAddresses.Add(new EventPipeCallStackAddress(address, methodFullName));
            }

            return new EventPipeCallStack(callStackAddresses);
        }

        return null;
    }

    private static string GetFullName(MethodLoadUnloadVerboseTraceData data)
    {
        var sig = data.MethodSignature;
        var parens = sig.IndexOf('(');
        var args = parens >= 0 ? sig.Substring(parens) : "";

        return data.MethodNamespace + "." + data.MethodName + args;
    }

    private class ParserMethod
    {
        public string FullName;
        public ulong StartAddress;
        public uint Length;

        public bool ContainsAddress(ulong address)
        {
            return address >= StartAddress && address < (StartAddress + Length);
        }
    }

    private class ParserMethodAddressComparer : IComparer<ParserMethod>
    {
        public static ParserMethodAddressComparer Instance { get; } = new();
        public int Compare(ParserMethod? x, ParserMethod? y)
        {
            return x!.StartAddress.CompareTo(y!.StartAddress);
        }
    }
}
