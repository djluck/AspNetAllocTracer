using System.Threading.Channels;
using AspNetAllocTracer.Example;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetAllocTracer.Benchmarks;

[SimpleJob(RunStrategy.Monitoring, iterationCount: 10, warmupCount: 0, invocationCount: 1, launchCount: 2)]
[GcServer(true)]
public class MultiRequestOverhead
{
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
        
        Console.WriteLine("Started web server..");
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
        
        Console.WriteLine("Started web server..");
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
        await SendRequests("http://localhost:12204/api/test/alloc-multi-namespace");
    }
    
    [Benchmark]
    public async Task WithAllocTracing()
    {
        await SendRequests("http://localhost:12204/api/test/alloc-multi-namespace");
    }

    private async Task SendRequests(string url, int count = 1_000, int concurrency = 50)
    {
        var chann = Channel.CreateBounded<int>(count);

        // setup consumers (aka request senders)
        var tasks = Enumerable.Range(1, concurrency)
            .Select(async _ =>
            {
                var buffer = new byte[1024 * 64];
                await foreach (var reqNum in chann.Reader.ReadAllAsync())
                {
                    using var r = await _httpClient.GetAsync(url);
                    r.EnsureSuccessStatusCode();
                    using var s = await r.Content.ReadAsStreamAsync();
                    while (await s.ReadAsync(buffer, 0, buffer.Length) > 0)
                    {
                    }
                }
            })
        .ToArray();
    
        // send messages 
        for (int i = 1; i <= count; i++)
            await chann.Writer.WriteAsync(i);

        chann.Writer.Complete();

        await Task.WhenAll(tasks);
    }
}