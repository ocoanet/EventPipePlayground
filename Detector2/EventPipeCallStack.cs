namespace Detector2;

public class EventPipeCallStack
{
    public EventPipeCallStack(EventPipeCallStackAddress[] addresses)
    {
        Addresses = addresses;
    }

    public EventPipeCallStackAddress[] Addresses { get; }
}
