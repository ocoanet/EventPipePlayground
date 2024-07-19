namespace Detector2;

public class EventPipeUnresolvedCallSack
{
    public EventPipeUnresolvedCallSack(ulong[] addresses)
    {
        Addresses = addresses;
    }

    public ulong[] Addresses { get; }
}
