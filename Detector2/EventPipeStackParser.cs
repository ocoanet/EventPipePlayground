using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Detector2;

public unsafe class EventPipeStackParser
{
    private readonly Dictionary<int, ParserThread> _threads = new();
    private int _eventCount;

    public EventPipeStackParser(EventPipeEventSource eventSource)
    {
        eventSource.AllEvents += OnAllEvents;
        eventSource.Clr.GCSampledObjectAllocation += OnGCSampledObjectAllocation;
    }

    private void OnAllEvents(TraceEvent traceEvent)
    {
        _eventCount++;
    }

    private void OnGCSampledObjectAllocation(GCSampledObjectAllocationTraceData traceData)
    {
         var traceEvent = (TraceEvent)traceData;
        var traceEventProxy = Unsafe.As<TraceEvent, TraceEventProxy>(ref traceEvent);
        if (traceEventProxy.eventRecord == null)
            return;

        var extendedData = traceEventProxy.eventRecord->ExtendedData;
        var extendedDataCount = traceEventProxy.eventRecord->ExtendedDataCount;

        for (var i = 0; i < extendedDataCount; i++)
        {
            if (extendedData[i].ExtType != TraceEventNativeMethods.EVENT_HEADER_EXT_TYPE_STACK_TRACE64)
                continue;

            int pointerSize = 8;
            var stackRecord = (TraceEventNativeMethods.EVENT_EXTENDED_ITEM_STACK_TRACE64*)extendedData[i].DataPtr;

            ulong* addresses = &stackRecord->Address[0];
            int addressesCount = (extendedData[i].DataSize - sizeof(ulong)) / pointerSize;

            // TraceProcess process = processes.GetOrCreateProcess(traceEvent.ProcessID, traceEvent.TimeStampQPC);
            // TraceThread thread = Threads.GetOrCreateThread(data.ThreadIDforStacks(), data.TimeStampQPC, process);
            var thread = GetOrCreateThread(traceEventProxy.ThreadIDforStacks(), traceEvent.TimeStampQPC);

            EventIndex eventIndex = (EventIndex)_eventCount;

            // TODO
            // CallStackIndex callStackIndex = callStacks.GetStackIndexForStackEvent(addresses, addressesCount, pointerSize, thread);
            // Debug.Assert(callStacks.Depth(callStackIndex) == addressesCount);

            // Note that we don't interfere with the splicing of kernel and user mode stacks because we do
            // see user mode stacks delayed and have a new style user mode stack spliced in.
            // TODO
            // AddStackToEvent(eventIndex, callStackIndex);
            // if (countForEvent != null)
            // {
            //     countForEvent.m_stackCount++; // Update stack counts
            // }
        }
    }

    private ParserThread GetOrCreateThread(int threadId, long timeStampQpc)
    {
        if (!_threads.TryGetValue(threadId, out var thread))
        {
            thread = new ParserThread
            {
                startTimeQPC = timeStampQpc,
            };
            _threads.Add(threadId, thread);
        }

        return thread;
    }

    public class ParserThread
    {
        internal long startTimeQPC;
    }
}
