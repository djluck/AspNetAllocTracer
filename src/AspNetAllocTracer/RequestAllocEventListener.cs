using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace AspNetAllocTracer;

public class AllocTracerEventListener : EventListener
{
    private static readonly Guid 
            EventSourceDotNetRuntime = Guid.Parse("e13c0d23-ccbc-4e12-931b-d9cc2eee27e4"),
            EventSourceKestrel = Guid.Parse("bdeb4676-a36e-5442-db99-4764e2326c7d");
    
    private static readonly int 
        EventIdAllocTick = 10, 
        EventIdRequestStart = 3, 
        EventIdRequestStop = 4;
    
    private readonly ILogger<AllocTracerEventListener>  _logger;
    private readonly ConcurrentDictionary<Guid, TracedRequest> _requests;
    private readonly ObjectPool<TracedRequest> _requestPool;
    private readonly AllocTracerOptions _options;

    public AllocTracerEventListener(ILogger<AllocTracerEventListener> logger, IOptions<AllocTracerOptions> options) : base ()
    {
        _logger = logger;
        _options = options.Value;
        _requests = new ConcurrentDictionary<Guid, TracedRequest>();
        _requestPool = new DefaultObjectPool<TracedRequest>(new TracedRequestPoolPolicy(), _options.MaxPoolSize);
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Guid == EventSourceDotNetRuntime)
        {
            // Enable verbose GC level event to be able to see allocations every ~100kb
            EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)0x1);
        }
        else if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
        {
            // Activity IDs aren't enabled by default.
            // Enabling Keyword 0x80 on the TplEventSource turns them on
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
        }
        else if (eventSource.Guid == EventSourceKestrel)
        {
            // Allows us to know when HTTP requests have started/ stopped
            EnableEvents(eventSource, EventLevel.Informational, EventKeywords.All);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // This can happen early on when tracing begins (events can arrive before constructor completes)
        if (_requests == null)
            return;
        
        try
        {
            if (eventData.EventSource.Guid == EventSourceKestrel && eventData.EventId == EventIdRequestStart)
            {
                var r = new Request((string) eventData.Payload![1]!, (string) eventData.Payload[4]!,
                    (string) eventData.Payload[3]!);

                if (!_options.TraceRequest(r))
                {
                    // We're not tracing this request!
                    return;
                }

                var req = _requestPool.Get();
                req.StartedAt = DateTime.UtcNow;
                req.Request = r;
                _requests.TryAdd(eventData.ActivityId, req);
            }
            else if (eventData.EventSource.Guid == EventSourceKestrel && eventData.EventId == EventIdRequestStop)
            {
                if (!_requests.TryRemove(eventData.ActivityId, out var req))
                    return;

                try
                {
                    req.FinishedAt = DateTime.UtcNow;

                    try
                    {
                        _options.TracedRequestsSubject.OnNext(req);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to notify all observers of allocations a traced HTTP request made");
                    }
                }
                finally
                {
                    _requestPool.Return(req);
                }
            }
            // AllocationTick event, fired every 100KB+ of allocations
            else if (eventData.EventSource.Guid == EventSourceDotNetRuntime && eventData.EventId == EventIdAllocTick)
            {
                if (!_requests.TryGetValue(eventData.ActivityId, out var req))
                    return;

                var allocBytes = (ulong) eventData.Payload![3]!;
                var typeName = (string) eventData.Payload[5]!;
                req.AddAlloc(typeName, allocBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trace a HTTP request/ failed to tie an allocation to a HTTP request");
        }
    }

    private class TracedRequestPoolPolicy : IPooledObjectPolicy<TracedRequest>
    {
        public TracedRequest Create() => new();

        public bool Return(TracedRequest obj)
        {
            obj.ClearState();
            return true;
        }
    }
}