using System.Diagnostics.Tracing;

namespace Allocator2;

[EventSource(Name = "EventPipePlayGround-Replay")]
public class ReplayEventSource : EventSource
{
    public static ReplayEventSource Log { get; } = new();

    public static string ProviderName => Log.Name;

    public const int EventStoreEventProcessingStartId = 1;

    [Event(EventStoreEventProcessingStartId, Level = EventLevel.Informational, Task = Tasks.EntryProcessing, Opcode = EventOpcode.Start)]
    public void EventStoreEventProcessingStart(long sequence, long timestampTicks)
        => WriteEvent(EventStoreEventProcessingStartId, sequence, timestampTicks);

    public const int EventStoreEventProcessingStopId = 2;

    [Event(EventStoreEventProcessingStopId, Level = EventLevel.Informational, Task = Tasks.EntryProcessing, Opcode = EventOpcode.Stop)]
    public void EventStoreEventProcessingStop()
        => WriteEvent(EventStoreEventProcessingStopId);

    public static class Tasks
    {
        public const EventTask EntryProcessing = (EventTask)1;
    }
}
