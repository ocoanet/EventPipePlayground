using System.Diagnostics.Tracing;

namespace Allocator2;

[EventSource(Name = "EventPipePlayGround-Replay")]
public class ReplayEventSource : EventSource
{
    public static ReplayEventSource Log { get; } = new();

    public static string ProviderName => Log.Name;

    public const int EventStoreEventProcessingStartId = 101;

    [Event(EventStoreEventProcessingStartId, Level = EventLevel.Informational, Task = (EventTask)1, Opcode = EventOpcode.Start)]
    public void EventStoreEventProcessingStart(long sequence, long timestampTicks)
        => WriteEvent(EventStoreEventProcessingStartId, sequence, timestampTicks);

    public const int EventStoreEventProcessingStopId = 102;

    [Event(EventStoreEventProcessingStopId, Level = EventLevel.Informational, Task = (EventTask)1, Opcode = EventOpcode.Stop)]
    public void EventStoreEventProcessingStop()
        => WriteEvent(EventStoreEventProcessingStopId);
}
