The projets in this repository explore multiple ways to capture all allocations stack traces using [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe). Capturing all allocation stack taces can be challenging because it requires activating CLR runtime provider keywords before the target application start. However, the public constructor of `DiagnosticsClient` takes a process ID, so the target application must be started before creating the event pipe session. Also, previously documented ways to extract stack traces for [ETW](https://github.com/microsoft/perfview/blob/main/documentation/TraceEvent/TraceEventProgrammersGuide.md), for example by using the `ClrStackWalkTraceData` events, are not available with EventPipe.

## Allocator 1

Allocator 1 is a dummy application which runs for a few seconds and allocates objects.

## Detector 1

- Use `dotnet-trace` to start the target applications.
- Convert the output nettrace file to ETLX using `TraceLog.CreateFromEventPipeDataFile`.
- Parse the ETLX file using `TraceLog` and extract stack traces using the `CallStack` trace event extension method.

## Detector 2 (WIP)

- Use `dotnet-trace` to start the target applications.
- Parse the  nettrace file using `EventPipeEventSource`.
- Use unsafe code to extract the stack traces from trace events.

## Detector 3 (failed expriment)

- Use [CreateProcessForLaunch](https://learn.microsoft.com/en-us/dotnet/core/unmanaged-api/debugging/createprocessforlaunch-function) to start the target application.
- Create a `DiagnosticsClient` attached to the target application process.
- Start an event pipe session.
- Resume the target application process.

## Detector 4 (WIP)

- Use `dotnet-trace` internal code to start the application process with a diagnostic post.
- Parse the event stream with `EventPipeEventSource`.
- Use unsafe code to extract the stack traces from trace events.
