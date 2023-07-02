using System.Reactive.Subjects;

namespace AspNetAllocTracer;

public class AllocTracerOptions
{
    /// <summary>
    /// Configures the maximum number of <see cref="TracedRequest"/> instances that will be pooled.
    /// </summary>
    /// <remarks>
    /// This value should be roughly equal to the throughput (requests processed/ sec) of your web server.
    /// </remarks>
    public int MaxPoolSize { get; set; } = 100;
   
    /// <summary>
    /// A predicate that controls what requests are traced or not 
    /// </summary>
    public Func<Request, bool> TraceRequest { get; set; } = r => true;

    /// <summary>
    /// Configures how reports around allocations are logged.
    /// </summary>
    public ReporterOptions ReporterOptions { get; set; } = new();
    
    /// <summary>
    /// An observable that you can subscribe to to receive notifications of all traced requests.
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="TracedRequest"/> are pooled to minimize tracing overhead- if you wish to use an instance
    /// outside the lifespan of your observer function, you will need to clone the object (use the <c>with</c> keyword).
    /// </remarks>
    public IObservable<TracedRequest> TracedRequests => TracedRequestsSubject;
    internal Subject<TracedRequest> TracedRequestsSubject { get; } = new Subject<TracedRequest>();
}

public class ReporterOptions
{
    /// <summary>
    /// If true, will log an allocation report after each traced request. Defaults to true.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Caps the number of types logged in the per-type breakdown. Defaults to 10.
    /// </summary>
    public int TypeCount { get; set; } = 10;
    
    /// <summary>
    /// Caps the number of namespaces logged in the per-namespace breakdown. Defaults to 5.
    /// </summary>
    public int NamespaceCount { get; set; } = 5;
    
    public int KbPrecision { get; set; } = 2;

    /// <summary>
    /// The minimum amount of memory a request has to allocate to be reported upon.
    /// Defaults to no limit.
    /// </summary>
    public uint? MinAllocThresholdBytes { get; set; }
}