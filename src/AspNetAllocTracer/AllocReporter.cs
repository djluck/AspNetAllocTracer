using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetAllocTracer;

public class AllocReporter : IObserver<TracedRequest>
{
    private readonly AllocLogger _logger;
    private readonly ReporterOptions _options;

    public AllocReporter(ReporterOptions options, ILogger<AllocReporter> logger)
    {
        _logger = new AllocLogger(logger);
        _options = options;
    }
    
    public AllocReporter(IOptions<AllocTracerOptions> options, ILogger<AllocReporter> logger)
        : this(options.Value.ReporterOptions, logger)
    {
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(TracedRequest req)
    {
        if (!_options.Enabled)
            return;

        if (_options.MinAllocThresholdBytes.HasValue && req.Allocated < _options.MinAllocThresholdBytes)
            return;
        
        try
        {
            // TODO threshold
            var typeBreakdown = req.Allocations
                .OrderByDescending(x => x.Value)
                .Select(x => new AllocationPerType(x.Key, Math.Round(x.Value / 1024.0, _options.KbPrecision)))
                .Take(_options.TypeCount);
            
            var namespaceBreakdown = req.Allocations
                .GroupBy(x => 
                    TryGetNamespace(x.Key, out var ns) ? ns : "unknown", 
                    (ns, typesAllocated) => new AllocationPerNamespace(ns, Math.Round(typesAllocated.Aggregate(0ul, (acc, next) => acc + next.Value) / 1024.0, _options.KbPrecision))
                )
                .OrderByDescending(x => x.AllocKB)
                .Take(_options.NamespaceCount);

            _logger.RequestTraced(req.Request.RequestId, req.Request.Path, req.Request.Verb, Math.Round(req.Allocated / 1024.0, _options.KbPrecision), typeBreakdown, namespaceBreakdown);
        }
        catch (Exception ex)
        {
            _logger.TracingFailed(ex, req.Request.RequestId, req.Request.Verb, req.Request.Path);
        }
    }
    
    private static bool TryGetNamespace(string typeName, [NotNullWhen(true)] out string? ns)
    {
        ns = null;
        var lastDotIndex = typeName.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
        if (lastDotIndex == -1)
            return false;
        
        ns = new string(typeName.AsSpan(0, lastDotIndex));
        return true;
    }

    internal record struct AllocationPerType(string TypeName, double AllocKB)
    {
        public override string ToString() => $"({TypeName}: {AllocKB}KB)";
    }
    
    internal record struct AllocationPerNamespace(string Namespace, double AllocKB)
    {
        public override string ToString() => $"({Namespace}: {AllocKB}KB)";
    }
}

internal partial class AllocLogger
{
    private readonly ILogger _logger;

    public AllocLogger(ILogger logger)
    {
        _logger = logger;
    }
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Request (Id={RequestId}, Verb={Verb}, Path={Path}) allocated {AllocatedKB:N0}KB. Allocations by type: {AllocPerType}. Allocations by namespace: {AllocPerNamespace}")]
    public partial void RequestTraced(string requestId, string path, string verb, double allocatedKB, IEnumerable<AllocReporter.AllocationPerType> allocPerType, IEnumerable<AllocReporter.AllocationPerNamespace> allocPerNamespace);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Failed to report allocations for a traced HTTP request (Id={RequestId}, Verb={Verb}, Path={Path})")]
    public partial void TracingFailed(Exception ex, string requestId, string path, string verb);
}