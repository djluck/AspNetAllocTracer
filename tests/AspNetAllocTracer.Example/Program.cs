
using AspNetAllocTracer.Example;

await using var app = App.Create(
    configureBuilder: cfg => cfg.Host.UseConsoleLifetime()
);
await app.RunAsync();