// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Microsoft.Diagnostics.Tracing;

public unsafe class TraceEventProxy
{
    /// <summary>
    /// EventTypeUserData is a field users get to use to attach their own data on a per-event-type basis.
    /// </summary>
    public object EventTypeUserData;
    internal bool NeedsFixup;
    internal int ParentThread;
    internal TraceEventNativeMethods.EVENT_RECORD* eventRecord;
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
