using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AspNetAllocTracer.Example;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace AspNetAllocTracer.Tests
{
    [TestFixture]
    public class Given_Tracing_Enabled_For_All_Requests
    {
        private HttpClient _httpClient;
        private IObservable<TracedRequest> _observable;
        private WebApplication _app;

        [SetUp]
        public void SetUp()
        {
            // Configure example server 
            _app = App.Create(new WebApplicationOptions()
            {
                ContentRootPath = "../../../../AspNetAllocTracer.Example"
            }, cfg =>
            {
                cfg.WebHost.ConfigureKestrel(x => x.ListenLocalhost(12203));
                cfg.Services.Configure<AllocTracerOptions>(cfg => { _observable = cfg.TracedRequests; });
                //cfg.Services.AddSingleton<ILogger<AllocReporter>>(new MockLogger());
            });

            // Start our server
            _app.RunAsync();
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:12203/")
            };
        }

        [TearDown]
        public async Task TearDown()
        {
            await _app.StopAsync();
            _httpClient.Dispose();
        }

        [Test]
        public async Task Can_Measure_Allocations_For_A_Request()
        {
            // arrange
            using var reqTask = _observable.FirstAsync().Select(x => x with { })
                .ToTask(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

            // act
            using var response = await _httpClient.GetAsync("/api/test/alloc");
            response.EnsureSuccessStatusCode();

            // assert
            var req = await reqTask;
            req.Request.Path.Should().Be("/api/test/alloc");
            req.Request.Verb.Should().Be("GET");
            req.Request.RequestId.Should().NotBeNullOrEmpty();
            req.Allocated.Should().BeGreaterThan(14_000_000);
            req.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            // Verify that the one big byte array alloc was tracked
            req.Allocations.Should().ContainKey("System.Byte[]").WhoseValue.Should().BeGreaterThanOrEqualTo(10_000_000);
            // Verify that the many smaller char array allocs were tracked
            req.Allocations.Should().ContainKey("System.Char[]").WhoseValue.Should().BeGreaterThanOrEqualTo(2_000_000);
        }

        [Test]
        public async Task Can_Measure_Allocations_For_A_Request_Path()
        {
            // arrange
            using var reqTask = _observable.FirstAsync().Select(x => x with { })
                .ToTask(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

            // act
            using var response = await _httpClient.GetAsync("/api/test/alloc?a=1&b=2s");
            response.EnsureSuccessStatusCode();

            // assert
            var req = await reqTask;
            req.Request.Path.Should().Be("/api/test/alloc");
            req.Request.Verb.Should().Be("GET");
            req.Request.RequestId.Should().NotBeNullOrEmpty();
            req.Allocated.Should().BeGreaterThan(13_000_000);
        }

        [Test]
        public async Task Can_Measure_Allocations_Across_Many_Types_For_Request()
        {
            // arrange
            using var reqTask = _observable.FirstAsync().Select(x => x with { })
                .ToTask(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

            // act
            using var response = await _httpClient.GetAsync("/api/test/alloc-multi-namespace");
            response.EnsureSuccessStatusCode();

            // assert
            var req = await reqTask;
            req.Request.Path.Should().Be("/api/test/alloc-multi-namespace");
            req.Request.Verb.Should().Be("GET");
            req.Request.RequestId.Should().NotBeNullOrEmpty();
            req.Allocated.Should().BeGreaterThan(10_000_000);
            req.Duration.Should().BeGreaterThan(TimeSpan.Zero);
            req.Allocations.Should().HaveCountGreaterOrEqualTo(50);
        }
    }

    [TestFixture]
    public class Given_Tracing_Enabled_For_Some_Requests
    {
        private HttpClient _httpClient;
        private IObservable<TracedRequest> _observable;
        private WebApplication _app;

        [SetUp]
        public void SetUp()
        {
            // Configure example server 
            _app = App.Create(new WebApplicationOptions()
            {
                ContentRootPath = "../../../../AspNetAllocTracer.Example"
            }, cfg =>
            {
                cfg.WebHost.ConfigureKestrel(x => x.ListenLocalhost(12203));
                cfg.Services.Configure<AllocTracerOptions>(cfg =>
                {
                    _observable = cfg.TracedRequests;
                    cfg.TraceRequest = req => req.Path != "/api/test/alloc-multi-namespace";
                });
            });

            // Start our server
            _app.RunAsync();
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:12203/")
            };
        }

        [TearDown]
        public async Task TearDown()
        {
            await _app.StopAsync();
            _httpClient.Dispose();
        }

        [Test]
        public async Task Can_Measure_Allocations_For_A_Non_Ignored_Request()
        {
            // arrange
            using var reqTask = _observable.FirstAsync().Select(x => x with { })
                .ToTask(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

            // act
            using var response = await _httpClient.GetAsync("/api/test/alloc");
            response.EnsureSuccessStatusCode();

            // assert
            var req = await reqTask;
            req.Request.Path.Should().Be("/api/test/alloc");
            req.Request.Verb.Should().Be("GET");
        }
        
        [Test]
        public async Task Wont_Measure_Allocations_For_Ignored_Requests()
        {
            // arrange
            using var reqTask = _observable.FirstAsync().Select(x => x with { })
                .ToTask(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

            // act
            using var response = await _httpClient.GetAsync("/api/test/alloc-multi-namespace");
            response.EnsureSuccessStatusCode();

            // assert
            try
            {
                var req = await reqTask;
                Assert.Fail("Expected cancellation!");
            }
            catch (TaskCanceledException){}
        }
    }
}