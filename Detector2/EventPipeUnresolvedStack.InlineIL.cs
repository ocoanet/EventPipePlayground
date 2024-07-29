using Microsoft.Diagnostics.Tracing;
using InlineIL;

namespace Detector2;

public unsafe partial class EventPipeUnresolvedStack
{
    /// <remarks>
    /// Use InlineIL.Fody to read field (requires IgnoresAccessChecksToGenerator).
    /// </remarks>
    public static EventPipeUnresolvedStack? ReadStackUsingInlineIL(TraceEvent traceEvent)
    {
        var eventRecord = ReadEventRecord(traceEvent);
        if (eventRecord == null)
            return null;

        return GetFromEventRecord(eventRecord);
    }

    private static TraceEventNativeMethods.EVENT_RECORD* ReadEventRecord(TraceEvent traceEvent)
    {
        IL.Emit.Ldarg(nameof(traceEvent));
        IL.Emit.Ldfld(FieldRef.Field(typeof(TraceEvent), "eventRecord"));
        IL.Emit.Ret();

        throw IL.Unreachable();
    }
}
