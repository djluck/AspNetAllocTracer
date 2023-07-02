using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetAllocTracer;

public class AllocTracerService : IHostedService
{
    private readonly ILogger<AllocTracerEventListener> _logger;
    private readonly AllocReporter _reporter;
    private readonly IOptions<AllocTracerOptions> _options;
    private AllocTracerEventListener? _listener;
    private IDisposable? _reporterSub;

    public AllocTracerService(ILogger<AllocTracerEventListener> logger, AllocReporter reporter, IOptions<AllocTracerOptions> options)
    {
        _logger = logger;
        _reporter = reporter;
        _options = options;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _reporterSub = _options.Value.TracedRequests.Subscribe(_reporter);
        _listener = new AllocTracerEventListener(_logger, _options);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _options.Value.TracedRequestsSubject.OnCompleted();
        _options.Value.TracedRequestsSubject.Dispose();
        _listener?.Dispose();
        _reporterSub?.Dispose();
    }
}