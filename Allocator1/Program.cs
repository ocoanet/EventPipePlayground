using System.Diagnostics;
using System.Runtime.CompilerServices;

var stopwatch = Stopwatch.StartNew();
var list = new List<Container>();
var flagCount = 0;

while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
{
    var result = Run();
    flagCount += result ? 1 : 0;
    Thread.Sleep(1);
}

Console.WriteLine($"Done, AllocationCount: {list.Count}, AllocationSize: {list.Sum(x => x.Data.Length)}, FlagCount: {flagCount}");

bool Run()
{
    if (Random.Shared.Next(3) == 0)
        list.Add(new Container());

    var flags = GetFlags();

    return IsF2(flags);
}

Flags GetFlags()
{
    return Random.Shared.Next(3) == 0 ? Flags.F1 : Flags.F2 | Flags.F3;
}

[MethodImpl(MethodImplOptions.NoInlining)]
bool IsF2(Flags flags1)
{
    return flags1.HasFlag(Flags.F2);
}

[Flags]
public enum Flags
{
    None = 0,
    F1 = 1,
    F2 = 2,
    F3 = 4,
}

public class Container
{
    public byte[] Data { get; } = new byte[1024];
}
