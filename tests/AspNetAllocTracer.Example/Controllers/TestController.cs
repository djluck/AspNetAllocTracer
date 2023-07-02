using AspNetAllocTracer.Example.Controllers.Types1;
using Microsoft.AspNetCore.Mvc;

namespace AspNetAllocTracer.Example.Controllers;

 /// <summary>
/// Our dummy API used for testing. Has to be in this namespace to be picked up by ASP.NET core.
/// </summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("alloc")]
    public async Task Alloc()
    {
        // alloc big chunks
        var b = new byte[1024 * 1024 * 10]; // 10MB
        
        // alloc lots of little chunks
        for (int i = 0; i < 100; i++)
        {
            var c = new char[20_000 + i];
        }

        await Task.Delay(10);
        // alloc lots of different types
    }

    [HttpGet("alloc-multi-namespace")]
    public async Task MultiNamespace()
    {
        // many small
        {
            var a = new A[1024 * 300];
            var b = new B[1024 * 300];
            var c = new C[1024 * 300];
            var d = new D[1024 * 300];
            var e = new E[1024 * 300];
            var f = new F[1024 * 300];
            var g = new G[1024 * 300];
            var h = new H[1024 * 300];
            var i = new I[1024 * 300];
            var j = new J[1024 * 300];
        }
        {
            var a = new global::AspNetAllocTracer.Example.Controllers.Types2.A[1024 * 300];
            var b = new global::AspNetAllocTracer.Example.Controllers.Types2.B[1024 * 300];
            var c = new global::AspNetAllocTracer.Example.Controllers.Types2.C[1024 * 300];
            var d = new global::AspNetAllocTracer.Example.Controllers.Types2.D[1024 * 300];
            var e = new global::AspNetAllocTracer.Example.Controllers.Types2.E[1024 * 300];
            var f = new global::AspNetAllocTracer.Example.Controllers.Types2.F[1024 * 300];
            var g = new global::AspNetAllocTracer.Example.Controllers.Types2.G[1024 * 300];
            var h = new global::AspNetAllocTracer.Example.Controllers.Types2.H[1024 * 300];
            var i = new global::AspNetAllocTracer.Example.Controllers.Types2.I[1024 * 300];
            var j = new global::AspNetAllocTracer.Example.Controllers.Types2.J[1024 * 300];
        }
        {
            var a = new global::AspNetAllocTracer.Example.Controllers.Types3.A[1024 * 300];
            var b = new global::AspNetAllocTracer.Example.Controllers.Types3.B[1024 * 300];
            var c = new global::AspNetAllocTracer.Example.Controllers.Types3.C[1024 * 300];
            var d = new global::AspNetAllocTracer.Example.Controllers.Types3.D[1024 * 300];
            var e = new global::AspNetAllocTracer.Example.Controllers.Types3.E[1024 * 300];
            var f = new global::AspNetAllocTracer.Example.Controllers.Types3.F[1024 * 300];
            var g = new global::AspNetAllocTracer.Example.Controllers.Types3.G[1024 * 300];
            var h = new global::AspNetAllocTracer.Example.Controllers.Types3.H[1024 * 300];
            var i = new global::AspNetAllocTracer.Example.Controllers.Types3.I[1024 * 300];
            var j = new global::AspNetAllocTracer.Example.Controllers.Types3.J[1024 * 300];
        }
        {
            var a = new global::AspNetAllocTracer.Example.Controllers.Types4.A[1024 * 300];
            var b = new global::AspNetAllocTracer.Example.Controllers.Types4.B[1024 * 300];
            var c = new global::AspNetAllocTracer.Example.Controllers.Types4.C[1024 * 300];
            var d = new global::AspNetAllocTracer.Example.Controllers.Types4.D[1024 * 300];
            var e = new global::AspNetAllocTracer.Example.Controllers.Types4.E[1024 * 300];
            var f = new global::AspNetAllocTracer.Example.Controllers.Types4.F[1024 * 300];
            var g = new global::AspNetAllocTracer.Example.Controllers.Types4.G[1024 * 300];
            var h = new global::AspNetAllocTracer.Example.Controllers.Types4.H[1024 * 300];
            var i = new global::AspNetAllocTracer.Example.Controllers.Types4.I[1024 * 300];
            var j = new global::AspNetAllocTracer.Example.Controllers.Types4.J[1024 * 300];
        }
        {
            var a = new global::AspNetAllocTracer.Example.Controllers.Types5.A[1024 * 300];
            var b = new global::AspNetAllocTracer.Example.Controllers.Types5.B[1024 * 300];
            var c = new global::AspNetAllocTracer.Example.Controllers.Types5.C[1024 * 300];
            var d = new global::AspNetAllocTracer.Example.Controllers.Types5.D[1024 * 300];
            var e = new global::AspNetAllocTracer.Example.Controllers.Types5.E[1024 * 300];
            var f = new global::AspNetAllocTracer.Example.Controllers.Types5.F[1024 * 300];
            var g = new global::AspNetAllocTracer.Example.Controllers.Types5.G[1024 * 300];
            var h = new global::AspNetAllocTracer.Example.Controllers.Types5.H[1024 * 300];
            var i = new global::AspNetAllocTracer.Example.Controllers.Types5.I[1024 * 300];
            var j = new global::AspNetAllocTracer.Example.Controllers.Types5.J[1024 * 300];
        }

        await Task.Delay(10);
    }
}
