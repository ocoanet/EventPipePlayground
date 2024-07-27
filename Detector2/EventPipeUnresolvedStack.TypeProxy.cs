using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tracing;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Detector2;

unsafe partial class EventPipeUnresolvedStack
{
    /// <remarks>
    /// Use type-proxy pattern to read field.
    /// </remarks>
    public static EventPipeUnresolvedStack? ReadStackUsingTypeProxy(TraceEvent traceEvent)
    {
        var traceEventProxy = Unsafe.As<TraceEvent, TraceEventProxy>(ref traceEvent);
        if (traceEventProxy.eventRecord == null)
            return null;

        return GetFromEventRecord(traceEventProxy.eventRecord);
    }

    private static EventPipeUnresolvedStack? GetFromEventRecord(EVENT_RECORD_PROXY* eventRecord)
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

            return new EventPipeUnresolvedStack(callStackAddresses);
        }

        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EVENT_RECORD_PROXY
    {
        public EVENT_HEADER_PROXY EventHeader;
        public ETW_BUFFER_CONTEXT_PROXY BufferContext;
        public ushort ExtendedDataCount;
        public ushort UserDataLength;
        public EVENT_HEADER_EXTENDED_DATA_ITEM_PROXY* ExtendedData;
        public IntPtr UserData;
        public IntPtr UserContext;
    }

    [StructLayout(LayoutKind.Explicit, Size = 80)]
    private struct EVENT_HEADER_PROXY
    {
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private struct ETW_BUFFER_CONTEXT_PROXY
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EVENT_HEADER_EXTENDED_DATA_ITEM_PROXY
    {
        public ushort Reserved1;
        public ushort ExtType;
        public ushort Reserved2;
        public ushort DataSize;
        public ulong DataPtr;
    };

    private class TraceEventProxy
    {
        /// <summary>
        /// EventTypeUserData is a field users get to use to attach their own data on a per-event-type basis.
        /// </summary>
        public object EventTypeUserData;
        internal bool NeedsFixup;
        internal int ParentThread;
        internal EVENT_RECORD_PROXY* eventRecord;
        internal IntPtr userData;
        /// <summary>
        /// TraceEvent knows where to dispatch to. To support many subscriptions to the same event we chain
        /// them.
        /// </summary>
        internal TraceEvent next;
        internal bool lookupAsClassic;
        internal bool lookupAsWPP;
        internal bool containsSelfDescribingMetadata;
        internal TraceEventID eventID;
        internal TraceEventOpcode opcode;
        internal string opcodeName;
        internal TraceEventTask task;
        internal string taskName;
        internal Guid taskGuid;
        internal Guid providerGuid;
        internal string providerName;
        internal bool eventNameIsJustTaskName;
        internal string eventName;
        /// <summary>
        /// The array of names for each property in the payload (in order).
        /// </summary>
        protected internal string[] payloadNames;
        internal TraceEventSource traceEventSource;
        internal EventIndex eventIndex;
        internal IntPtr myBuffer;
        internal string instanceContainerID;
    }
}
