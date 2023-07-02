# AspNetAllocTracer
Traces the memory allocated by individual ASP.NET core requests, logging allocation reports after each requests. This can be used to identitfy requests that might be allocating too much memory. Sample output:
```
info: AspNetAllocTracer.AllocReporter[0]
      Request (Id=0HMRQMHDMUQ7R:00000002, Verb=GET, Path=/api/product/beans) allocated 14,534KB. Allocations by type: [(System.Byte[]: 10336.07KB), (System.Char[]: 3990.39KB), (MemberInfoCache`1[System.Reflection.RuntimeConstructorInfo]: 103.7KB), (System.Reflection.Emit.DynamicILGenerator: 103.7KB)]. Allocations by namespace: [(System: 14326.46KB), (MemberInfoCache`1[System.Reflection: 103.7KB), (System.Reflection.Emit: 103.7KB)]
```

The method of tracing used by this package [has some limitations](#limitations) which users should be aware of.

## Using it
### Requirements
Requires:
- ASP.NET core 6.0
- The [Kestrel web server](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-6.0)

### Installation
The package is available on nuget:
```
dotnet add package AspNetAllocTracer
```

You can then enable it your ASP.NET application in the `Startup` configuration:
```
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddAllocationTracing()
}
```

### Configuration
This library has a few configuration knobs to tune the requests traced and the output generated:
```
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddAllocationTracing(cfg => {
        // Filter what requests are traced
        cfg.TraceRequest = req => req.Path != "/health";
        // Minimum a request has to allocate for it to be reported on
        cfg.ReporterOptions.MinAllocThresholdBytes = 1_000_000;
        // The number of types logged in the per-type breakdown
        cfg.ReporterOptions.TypeCount = 20;
        // The number of namespace logged in the per-namespace breakdown
        cfg.ReporterOptions.NamespaceCount = 10;
	});
}
```

#### Custom reporters
It's possible to write your own reporter (e.g. for recording metrics around the volume of memory allocated by requests):
```
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddAllocationTracing(cfg => {
        cfg.TracedRequests.Subscribe(tracedReq =>
        {
            // Do logic here
        });
	});
}	
```

## Limitations
The underlying tracing technology used is the [EventSource](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource) class which has a few limitations:
- Accuracy: memory allocations are only tracked every 100KB, so requests that allocate less than 100KB may not be tracked. 
  This also means that allocations can be incorrectly attributed to a single type (in 100KB of allocations, only the most recently allocated type name is reported upon).
- Performance: Enabling tracing has a small impact on performance of an application. It's not recommended that you leave this enabled constantly without understanding the performance impact on your application.