using AspNetAllocTracer.Example;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetAllocTracer.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, iterationCount: 100, warmupCount: 2, invocationCount: 10, launchCount: 3)]
[Config(typeof(Config))]
[EtwProfiler]
[GcServer(true)]
public class SingleRequestOverhead
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(
                StatisticColumn.P50,
                StatisticColumn.P90,
                StatisticColumn.P95,
                StatisticColumn.P100);
            // Add(new EtwProfiler(new EtwProfilerConfig
            // {
            //     // TODO understand why we are allocating so much extra memory
            //     Providers = new (Guid, TraceEventLevel, ulong, TraceEventProviderOptions)[1]
            //     {
            //         // ulong.MaxValue
            //         (ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, 167993UL,  new TraceEventProviderOptions()
            //         {
            //             StacksEnabled = false
            //         })
            //     }
            // }));
        }
    }

    private WebApplication _app;
    private HttpClient _httpClient;
            
    [GlobalSetup(Target = nameof(WithAllocTracing))]
    public void SetupWithAllocTracing()
    {
        // Configure example server 
        var _app = App.Create(new WebApplicationOptions()
        {
            ContentRootPath = "../../../../../../../../AspNetAllocTracer.Example"
        }, cfg =>
        {
            cfg.WebHost.ConfigureKestrel(x => x.ListenLocalhost(12204));
            cfg.Logging.ClearProviders();
        });
        
        // Start our server
        _app.RunAsync();
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:12204/")
        };
    }
    
    [GlobalSetup(Target = nameof(WithoutAllocTracing))]
    public void SetupWithoutAllocTracing()
    {
        // Configure example server 
        var _app = App.Create(new WebApplicationOptions()
        {
            ContentRootPath = "../../../../../../../../AspNetAllocTracer.Example"
        }, cfg =>
        {
            cfg.WebHost.ConfigureKestrel(x => x.ListenLocalhost(12204));
            cfg.Logging.ClearProviders();
        }, enabledAllocTracing: false);
        
        // Start our server
        _app.RunAsync();
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:12204/")
        };
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_app != null)
            await _app?.StopAsync();
        
        _httpClient?.Dispose();
    }
    
    [Benchmark(Baseline = true)]
    public async Task WithoutAllocTracing()
    {
        using var response = await _httpClient.GetAsync("http://localhost:12204/api/test/alloc-multi-namespace");
        response.EnsureSuccessStatusCode();
    }
    
    [Benchmark]
    public async Task WithAllocTracing()
    {
        using var response = await _httpClient.GetAsync("http://localhost:12204/api/test/alloc-multi-namespace");
        response.EnsureSuccessStatusCode();
    }
}