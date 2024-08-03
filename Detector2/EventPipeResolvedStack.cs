using System.Text;

namespace Detector2;

public class EventPipeResolvedStack
{
    public EventPipeResolvedStack(EventPipeResolvedAddress[] addresses)
    {
        Addresses = addresses;
    }

    public EventPipeResolvedAddress[] Addresses { get; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        foreach (var address in Addresses)
        {
            stringBuilder.AppendLine($"     {address}");
        }

        return stringBuilder.ToString();
    }
}

public readonly record struct EventPipeResolvedAddress(ulong CodeAddress, string? MethodFullName)
{
    public override string ToString()
    {
        return string.IsNullOrEmpty(MethodFullName) ? $"0x{CodeAddress:x}" : MethodFullName;
    }
}
