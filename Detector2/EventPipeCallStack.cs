namespace Detector2;

public class EventPipeCallStack
{
    public EventPipeCallStack(EventPipeCallStackAddress[] addresses)
    {
        Addresses = addresses;
    }

    public EventPipeCallStackAddress[] Addresses { get; }
}

public readonly record struct EventPipeCallStackAddress(ulong CodeAddress, string? MethodFullName)
{
    public override string ToString()
    {
        return string.IsNullOrEmpty(MethodFullName) ? $"0x{CodeAddress:x}" : MethodFullName;
    }
}
