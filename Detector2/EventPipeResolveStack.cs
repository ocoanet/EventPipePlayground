namespace Detector2;

public class EventPipeResolveStack
{
    public EventPipeResolveStack(EventPipeResolvedAddress[] addresses)
    {
        Addresses = addresses;
    }

    public EventPipeResolvedAddress[] Addresses { get; }
}

public readonly record struct EventPipeResolvedAddress(ulong CodeAddress, string? MethodFullName)
{
    public override string ToString()
    {
        return string.IsNullOrEmpty(MethodFullName) ? $"0x{CodeAddress:x}" : MethodFullName;
    }
}
