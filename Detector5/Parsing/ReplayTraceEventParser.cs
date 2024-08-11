using System.Diagnostics.Tracing;
using Allocator2;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace Detector5.Parsing;

public class ReplayTraceEventParser : TraceEventParser
{
    public static Guid ProviderGuid => TraceEventProviders.GetEventSourceGuidFromName(ReplayEventSource.ProviderName);
    public static string ProviderName => ReplayEventSource.ProviderName;

    private static volatile TraceEvent[] _templates;

    public ReplayTraceEventParser(TraceEventSource source, bool dontRegister = false)
        : base(source, dontRegister)
    {
    }

    public event Action<EventStoreEventProcessingStartTraceEvent> EventStoreEventProcessingStart
    {
        add => source.RegisterEventTemplate(CreateEventStoreEventProcessingStartTraceEvent(value));
        remove => source.UnregisterEventTemplate(value, ReplayEventSource.EventStoreEventProcessingStartId, ProviderGuid);
    }

    public event Action<EmptyTraceData> EventStoreEventProcessingStop
    {
        add => source.RegisterEventTemplate(CreateEventStoreEventProcessingStopTraceEvent(value));
        remove => source.UnregisterEventTemplate(value, ReplayEventSource.EventStoreEventProcessingStopId, ProviderGuid);
    }

    protected override string GetProviderName() => ReplayEventSource.ProviderName;

    protected override void EnumerateTemplates(Func<string, string, EventFilterResponse> eventsToObserve, Action<TraceEvent> callback)
    {
        if (_templates == null)
        {
            _templates =
            [
                CreateEventStoreEventProcessingStartTraceEvent(null),
                CreateEventStoreEventProcessingStopTraceEvent(null),
            ];
        }

        foreach (var template in _templates)
        {
            if (eventsToObserve == null || eventsToObserve(template.ProviderName, template.EventName) == EventFilterResponse.AcceptEvent)
            {
                callback(template);
            }
        }
    }

    private static EventStoreEventProcessingStartTraceEvent CreateEventStoreEventProcessingStartTraceEvent(Action<EventStoreEventProcessingStartTraceEvent>? action)
    {
        return new EventStoreEventProcessingStartTraceEvent(action, ReplayEventSource.EventStoreEventProcessingStartId, (int)ReplayEventSource.Tasks.EntryProcessing, nameof(ReplayEventSource.Tasks.EntryProcessing), Guid.Empty, (int)EventOpcode.Start, nameof(EventOpcode.Start), ProviderGuid, ProviderName);
    }

    private static EmptyTraceData CreateEventStoreEventProcessingStopTraceEvent(Action<EmptyTraceData>? action)
    {
        return new EmptyTraceData(action, ReplayEventSource.EventStoreEventProcessingStopId, (int)ReplayEventSource.Tasks.EntryProcessing, nameof(ReplayEventSource.Tasks.EntryProcessing), Guid.Empty, (int)EventOpcode.Stop, nameof(EventOpcode.Stop), ProviderGuid, ProviderName);
    }
}
