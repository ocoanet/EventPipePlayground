using System.Diagnostics;
using Microsoft.Diagnostics.Tracing;

namespace Detector5.Parsing;

public class EventStoreEventProcessingStartTraceEvent : TraceEvent
{
    private static readonly string[] _payloadNames = [nameof(Sequence), nameof(TimestampTicks)];

    private Action<EventStoreEventProcessingStartTraceEvent>? _target;

    public EventStoreEventProcessingStartTraceEvent(Action<EventStoreEventProcessingStartTraceEvent>? action, int eventId, int task, string taskName, Guid taskGuid, int opcode, string opcodeName, Guid providerGuid, string providerName)
        : base(eventId, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
    {
        _target = action;
    }

    public long Sequence => GetInt64At(0);
    public long TimestampTicks => GetInt64At(8);

    public override object? PayloadValue(int index)
    {
        return index switch
        {
            0 => Sequence,
            1 => TimestampTicks,
            _ => null,
        };
    }

    public override string[] PayloadNames => _payloadNames;

    protected override Delegate Target
    {
        get => _target;
        set => _target = (Action<EventStoreEventProcessingStartTraceEvent>)value;
    }

    protected override void Dispatch()
    {
        _target?.Invoke(this);
    }

    protected override void Validate()
    {
        Debug.Assert(Version == 0 && EventDataLength == 8 + 8);
    }
}
