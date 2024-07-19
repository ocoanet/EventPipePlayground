using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing;

namespace Detector2;

public static unsafe class EventPipeCallStackExtractor
{
    public static EventPipeUnresolvedCallSack? GetCallStack(TraceEvent traceEvent)
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
            if (addressCount == 0)
                return null;

            var callStackAddresses = new ulong[addressCount];
            for (var index = 0; index < addressCount; index++)
            {
                callStackAddresses[index] = addresses[index];
            }

            return new EventPipeUnresolvedCallSack(callStackAddresses);
        }

        return null;
    }

    // TODO: understand why UnsafeAccessor fails
    // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "eventRecord")]
    // private static extern ref TraceEventNativeMethods.EVENT_RECORD* GetEventRecord(TraceEvent traceEvent);
}
