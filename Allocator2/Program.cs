using System.Diagnostics;
using Allocator2;

var stopwatch = Stopwatch.StartNew();
var list1 = new List<Container1>();
var list2 = new List<Container2>();
var sequence = 0l;

while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
{
    Run(sequence);
    sequence++;
    Thread.Sleep(1);
}

Console.WriteLine($"Done, AllocationCount: {list1.Count + list2.Count}, AllocationSize: {list1.Sum(x => x.Data.Length) + list2.Sum(x => x.Data.Length)}");

void Run(long s)
{
    list1.Add(new Container1());

    if (Random.Shared.Next(3) == 0)
    {
        ReplayEventSource.Log.EventStoreEventProcessingStart(s, DateTime.UtcNow.Ticks);
        list2.Add(new Container2());
        ReplayEventSource.Log.EventStoreEventProcessingStop();
    }
}

public class Container1
{
    public byte[] Data { get; } = new byte[1024];
}

public class Container2
{
    public byte[] Data { get; } = new byte[1024];
}
