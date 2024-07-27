using System.Reflection;
using InlineIL;
using Microsoft.Diagnostics.Tracing;

namespace Detector2;

public unsafe partial class EventPipeStack
{
    public EventPipeStack(ulong[] addresses)
    {
        Addresses = addresses;
    }

    public ulong[] Addresses { get; }

    public static EventPipeStack? ReadFrom(TraceEvent traceEvent) => ReadStackUsingInlineIL(traceEvent);

    /// <remarks>
    /// Use reflection to read field.
    /// </remarks>
    public static EventPipeStack? ReadStackUsingReflection(TraceEvent traceEvent)
    {
        // Of course the FieldInfo needs to be cached.
        var fieldInfo = typeof(TraceEvent).GetField("eventRecord", BindingFlags.Instance | BindingFlags.NonPublic);
        var value = (Pointer?)fieldInfo!.GetValue(traceEvent);
        if (value == null)
            return null;

        var eventRecord = (TraceEventNativeMethods.EVENT_RECORD*)Pointer.Unbox(value);

        return GetFromEventRecord(eventRecord);
    }

    /// <remarks>
    /// Use InlineIL.Fody to read field.
    /// </remarks>
    public static EventPipeStack? ReadStackUsingInlineIL(TraceEvent traceEvent)
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

    private static unsafe EventPipeStack? GetFromEventRecord(TraceEventNativeMethods.EVENT_RECORD* eventRecord)
    {
        if (eventRecord == null)
            return null;

        var extendedData = eventRecord->ExtendedData;
        var extendedDataCount = eventRecord->ExtendedDataCount;

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

            return new EventPipeStack(callStackAddresses);
        }

        return null;
    }
}
