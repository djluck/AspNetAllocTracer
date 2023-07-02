namespace AspNetAllocTracer;

/// <summary>
/// Contains a breakdown of the memory allocated during the lifespan of an asp.net request.
/// </summary>
public record TracedRequest
{
    private object _key = new object();
    private Dictionary<string, ulong> _allocations = new();
    
#nullable disable
    public TracedRequest()
    {
    }
#nullable enable
    
    /// <summary>
    /// Clone constructor, called when the record is cloned
    /// </summary>
    /// <param name="req"></param>
    protected TracedRequest(TracedRequest req)
    {
        Request = req.Request;
        Allocated = req.Allocated;
        StartedAt = req.StartedAt;
        FinishedAt = req.FinishedAt;
        _allocations = new Dictionary<string, ulong>(req.Allocations);
    }

    public Request Request { get; internal set; }
    public DateTime StartedAt { get; internal set; }
    public DateTime FinishedAt { get; internal set; }
    public TimeSpan Duration => FinishedAt.Subtract(StartedAt);
    public ulong Allocated { get; private set; }
    
    /// <summary>
    /// Map of allocated types to amount in bytes
    /// </summary>
    public IReadOnlyDictionary<string, ulong> Allocations => _allocations;

    internal void AddAlloc(string typeName, ulong allocBytes)
    {
        lock (_key)
        {
            if (_allocations.TryGetValue(typeName, out var alloc))
                _allocations[typeName] = alloc + allocBytes;
            else
                _allocations.Add(typeName, allocBytes);
            
            Allocated += allocBytes;
        }
    }

    internal void ClearState()
    {
#nullable disable
        Request = default;
#nullable enable
        StartedAt = FinishedAt = default;
        Allocated = 0L;
        _allocations.Clear();
    }
}

public record struct Request(string RequestId, string Verb, string Path);