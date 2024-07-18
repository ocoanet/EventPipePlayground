namespace Detector2;

public class EventPipeCallStack
{
    public EventPipeCallStack(List<EventPipeCallStackAddress> addresses)
    {
        Addresses = addresses;
    }

    public List<EventPipeCallStackAddress> Addresses { get; }
}

public readonly record struct EventPipeCallStackAddress(ulong CodeAddress, string? MethodFullName)
{
    public override string ToString()
    {
        return string.IsNullOrEmpty(MethodFullName) ? $"0x{CodeAddress:x}" : MethodFullName;
    }
}
