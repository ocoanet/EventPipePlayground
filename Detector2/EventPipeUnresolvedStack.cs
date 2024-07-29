using System.Reflection;
using System.Runtime.CompilerServices;
using InlineIL;
using Microsoft.Diagnostics.Tracing;

namespace Detector2;

/// <summary>
/// Unresolved call stack.
/// </summary>
public unsafe partial class EventPipeUnresolvedStack
{
    public EventPipeUnresolvedStack(ulong[] addresses)
    {
        Addresses = addresses;
    }

    public ulong[] Addresses { get; }

    public static EventPipeUnresolvedStack? ReadFrom(TraceEvent traceEvent) => ReadStackUsingInlineIL(traceEvent);

    /// <remarks>
    /// Use reflection to read field.
    /// </remarks>
    public static EventPipeUnresolvedStack? ReadStackUsingReflection(TraceEvent traceEvent)
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
    /// Use UnsafeAccessor to read field (BROKEN).
    /// </remarks>
    public static EventPipeUnresolvedStack? ReadStackUsingUnsafeAccessor(TraceEvent traceEvent)
    {
        var eventRecord = (TraceEventNativeMethods.EVENT_RECORD*)GetEventRecord_1(ref traceEvent);

        return GetFromEventRecord(eventRecord);
    }

    /// <summary>
    /// Broken: generates <see cref="System.MissingFieldException"/>.
    /// </summary>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "eventRecord")]
    private static extern ref IntPtr GetEventRecord_1(ref TraceEvent traceEvent);

    /// <summary>
    /// Broken: generates runtime errors.
    /// <code>
    /// Failed to dereference an unmanaged pointer: A reference value was found to be bad during dereferencing.
    /// (0x80131305). The error code is CORDBG_E_BAD_REFERENCE_VALUE, or 0x80131305.
    /// </code>
    /// </summary>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "eventRecord")]
    private static extern ref TraceEventNativeMethods.EVENT_RECORD* GetEventRecord_2(ref TraceEvent traceEvent);

    /// <remarks>
    /// Use InlineIL.Fody to read field.
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

    private static EventPipeUnresolvedStack? GetFromEventRecord(TraceEventNativeMethods.EVENT_RECORD* eventRecord)
    {
        if (eventRecord == null)
            return null;

        var extendedDataCount = eventRecord->ExtendedDataCount;

        for (var dataIndex = 0; dataIndex < extendedDataCount; dataIndex++)
        {
            var extendedData = eventRecord->ExtendedData[dataIndex];
            if (extendedData.ExtType != TraceEventNativeMethods.EVENT_HEADER_EXT_TYPE_STACK_TRACE64)
                continue;

            var stackRecord = (TraceEventNativeMethods.EVENT_EXTENDED_ITEM_STACK_TRACE64*)extendedData.DataPtr;

            var addresses = &stackRecord->Address[0];
            var addressCount = (extendedData.DataSize - sizeof(ulong)) / sizeof(ulong);
            if (addressCount == 0)
                return null;

            var callStackAddresses = new ulong[addressCount];
            for (var index = 0; index < addressCount; index++)
            {
                callStackAddresses[index] = addresses[index];
            }

            return new EventPipeUnresolvedStack(callStackAddresses);
        }

        return null;
    }
}
