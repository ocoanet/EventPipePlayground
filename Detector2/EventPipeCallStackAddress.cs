namespace Detector2;

public readonly record struct EventPipeCallStackAddress(ulong CodeAddress, string? MethodFullName)
{
    public override string ToString()
    {
        return string.IsNullOrEmpty(MethodFullName) ? $"0x{CodeAddress:x}" : MethodFullName;
    }
}
