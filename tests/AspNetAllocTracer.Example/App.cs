namespace AspNetAllocTracer.Example;

public static class App
{
    public static WebApplication Create(WebApplicationOptions options = null, Action<WebApplicationBuilder> configureBuilder = null, bool enabledAllocTracing = true)
    {
        var builder = WebApplication.CreateBuilder(options);
        
        // Add services to the container.
        builder.Services.AddControllers().AddApplicationPart(typeof(App).Assembly);
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        if (enabledAllocTracing )
            builder.Services.AddAllocationTracing(cfg =>
            {
                cfg.TracedRequests.Subscribe(tracedReq =>
                {
                    // Do logic here
                });
            });
        if (configureBuilder != null)
            configureBuilder(builder);
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseRouting();
        app.UseEndpoints(e =>
        {
            e.MapControllers();
        });
        
        return app;
    }
}