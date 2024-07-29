using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing;

namespace Detector2;

public unsafe partial class EventPipeUnresolvedStack
{
    /// <remarks>
    /// Use UnsafeAccessor to read field (requires IgnoresAccessChecksToGenerator).
    /// </remarks>
    public static EventPipeUnresolvedStack? ReadStackUsingUnsafeAccessor(TraceEvent traceEvent)
    {
        var eventRecord = GetEventRecord(traceEvent);

        return GetFromEventRecord(eventRecord);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "eventRecord")]
    private static extern ref TraceEventNativeMethods.EVENT_RECORD* GetEventRecord(TraceEvent traceEvent);
}
