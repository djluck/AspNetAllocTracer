using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace AspNetAllocTracer.Tests;

public class AllocReporterTests
{
    private MockLogger _logger;

    [SetUp]
    public void SetUp()
    {
        _logger = new MockLogger();
    }

    [Test]
    public void Single_Type()
    {
        var reporter = new AllocReporter(new ReporterOptions(), _logger);
        var tracedRequest = new TracedRequest()
        {
            Request = new Request("test-req", "GET", "/some/path"),
            FinishedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddSeconds(-1)
        };
        tracedRequest.AddAlloc("System.Test", 20_000);
        reporter.OnNext(tracedRequest);

        var state = _logger.LoggedStates.Should().ContainSingle().Subject;

        state.Should().Contain("RequestId", "test-req");
        state.Should().Contain("Path", "/some/path");
        state.Should().Contain("Verb", "GET");
        state.Should().Contain("AllocatedKB", 19.53);
        ((IEnumerable<AllocReporter.AllocationPerType>) state.Should().ContainKey("AllocPerType").WhoseValue)
            .Should().ContainEquivalentOf(
                new AllocReporter.AllocationPerType("System.Test", 19.53)
            );
            
        ((IEnumerable<AllocReporter.AllocationPerNamespace>) state.Should().ContainKey("AllocPerNamespace").WhoseValue)
            .Should().ContainEquivalentOf(
                new AllocReporter.AllocationPerNamespace("System", 19.53)
            );
    }
    
    [Test]
    public void Multiple_Types()
    {
        var reporter = new AllocReporter(new ReporterOptions(), _logger);
        var tracedRequest = new TracedRequest()
        {
            Request = new Request("test-req", "GET", "/some/path"),
            FinishedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddSeconds(-1)
        };
        tracedRequest.AddAlloc("System.Test1", 20_000);
        tracedRequest.AddAlloc("System.Test2", 50_000);
        tracedRequest.AddAlloc("System.Test3", 30_000);
        tracedRequest.AddAlloc("System.SubNamespace.Test1", 100_000);
        tracedRequest.AddAlloc("System.SubNamespace.Test2", 55_000);
        reporter.OnNext(tracedRequest);

        var state = _logger.LoggedStates.Should().ContainSingle().Subject;
        
        state.Should().Contain("AllocatedKB", 249.02);
        ((IEnumerable<AllocReporter.AllocationPerType>) state.Should().ContainKey("AllocPerType").WhoseValue)
            .Should().ContainInOrder(
                new AllocReporter.AllocationPerType("System.SubNamespace.Test1", 97.66),
                new AllocReporter.AllocationPerType("System.SubNamespace.Test2", 53.71),
                new AllocReporter.AllocationPerType("System.Test2", 48.83),
                new AllocReporter.AllocationPerType("System.Test3", 29.3),
                new AllocReporter.AllocationPerType("System.Test1", 19.53)
            );
            
        ((IEnumerable<AllocReporter.AllocationPerNamespace>) state.Should().ContainKey("AllocPerNamespace").WhoseValue)
            .Should().ContainInOrder(
                new AllocReporter.AllocationPerNamespace("System.SubNamespace", 151.37),
                new AllocReporter.AllocationPerNamespace("System", 97.66)
            );
    }
    
    [Test]
    public void Limited_Multiple_Types()
    {
        var reporter = new AllocReporter(new ReporterOptions() { NamespaceCount = 1, TypeCount = 3}, _logger);
        var tracedRequest = new TracedRequest()
        {
            Request = new Request("test-req", "GET", "/some/path"),
            FinishedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddSeconds(-1)
        };
        tracedRequest.AddAlloc("System.Test1", 20_000);
        tracedRequest.AddAlloc("System.Test2", 50_000);
        tracedRequest.AddAlloc("System.Test3", 30_000);
        tracedRequest.AddAlloc("System.SubNamespace.Test1", 100_000);
        tracedRequest.AddAlloc("System.SubNamespace.Test2", 55_000);
        reporter.OnNext(tracedRequest);

        var state = _logger.LoggedStates.Should().ContainSingle().Subject;
        
        state.Should().Contain("AllocatedKB", 249.02);
        ((IEnumerable<AllocReporter.AllocationPerType>) state.Should().ContainKey("AllocPerType").WhoseValue)
            .Should().ContainInOrder(
                new AllocReporter.AllocationPerType("System.SubNamespace.Test1", 97.66),
                new AllocReporter.AllocationPerType("System.SubNamespace.Test2", 53.71),
                new AllocReporter.AllocationPerType("System.Test2", 48.83)
            );
            
        ((IEnumerable<AllocReporter.AllocationPerNamespace>) state.Should().ContainKey("AllocPerNamespace").WhoseValue)
            .Should().ContainInOrder(
                new AllocReporter.AllocationPerNamespace("System.SubNamespace", 151.37)
            );
    }
    
    [Test]
    public void Dont_Report_On_Low_Alloc_Requests()
    {
        var reporter = new AllocReporter(new ReporterOptions() { MinAllocThresholdBytes = 500_000 }, _logger);
        var tracedRequest = new TracedRequest()
        {
            Request = new Request("test-req", "GET", "/some/path"),
            FinishedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddSeconds(-1)
        };
        tracedRequest.AddAlloc("System.Test1", 20_000);
        tracedRequest.AddAlloc("System.Test2", 50_000);
        reporter.OnNext(tracedRequest);

        _logger.LoggedStates.Should().BeEmpty();
    }

    [Test]
    public void Do_Report_On_High_Alloc_Requests()
    {
        var reporter = new AllocReporter(new ReporterOptions() { MinAllocThresholdBytes = 500_000 }, _logger);
        var tracedRequest = new TracedRequest()
        {
            Request = new Request("test-req", "GET", "/some/path"),
            FinishedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddSeconds(-1)
        };
        tracedRequest.AddAlloc("System.Test1", 300_000);
        tracedRequest.AddAlloc("System.Test2", 201_000);
        reporter.OnNext(tracedRequest);

        _logger.LoggedStates.Should().NotBeEmpty();
    }
    
    public class MockLogger : ILogger<AllocReporter>
    {
        public List<Dictionary<string, object>> LoggedStates { get; set; } = new List<Dictionary<string, object>>();

        private readonly ILogger<AllocReporter> ConsoleLogger =
            LoggerFactory.Create(cfg => cfg.AddConsole()).CreateLogger<AllocReporter>();

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ConsoleLogger.Log(logLevel, eventId, state, exception, formatter);
            if (logLevel == LogLevel.Information)
            {
                LoggedStates.Add(
                    ((IReadOnlyList<KeyValuePair<string, object>>) state).ToDictionary(k => k.Key, v => v.Value));
            }
            else
                Assert.Fail($"Unexpected log level: {logLevel}. Exception: {exception}");
        }
    }
}