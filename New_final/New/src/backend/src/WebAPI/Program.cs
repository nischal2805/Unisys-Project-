using Application;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text.Json;
using WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Arazzo Workflow Platform API", Version = "v1" });
});

// Configure Kestrel for longer request timeouts (AI generation can take time)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(15);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(15);
});

// Add CORS - Allow all origins in development, specific origins in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", 
                "http://localhost:3001",
                "http://frontend:80",
                "http://arazzo-frontend:80")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add OpenTelemetry tracing (enabled when Jaeger endpoint is configured)
var jaegerEndpoint = builder.Configuration.GetValue<string>("Jaeger:Endpoint");
if (!string.IsNullOrEmpty(jaegerEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource("WebAPI")
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("arazzo-backend", serviceVersion: "1.0.0"))
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(jaegerEndpoint);
                });
        });
    Log.Information("OpenTelemetry tracing enabled with Jaeger at {Endpoint}", jaegerEndpoint);
}

try
{
    Log.Information("Building application...");
    var app = builder.Build();
    
    // Ensure database is created and migrations are applied
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            dbContext.Database.EnsureCreated();
            Log.Information("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database initialization warning (may already exist)");
        }
    }
    
    Log.Information("Application built successfully");

    // Configure the HTTP request pipeline
    
    // Exception handling should be first to catch all exceptions
    app.UseExceptionHandling();
    
    // Request logging after exception handling
    app.UseRequestLogging();
    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Arazzo Workflow Platform API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseSerilogRequestLogging();

    // Use AllowAll for development, AllowFrontend for production
    var env = app.Environment;
    app.UseCors(env.IsDevelopment() ? "AllowAll" : "AllowFrontend");

    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint with detailed status
    app.MapGet("/health", async (ApplicationDbContext db) =>
    {
        var dbHealthy = false;
        try
        {
            await db.Database.CanConnectAsync();
            dbHealthy = true;
        }
        catch { }
        
        return Results.Ok(new 
        { 
            status = dbHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            services = new
            {
                database = dbHealthy ? "connected" : "disconnected"
            }
        });
    });

    Log.Information("Starting WebAPI on http://localhost:5000...");
    Log.Information("Swagger UI available at: http://localhost:5000/swagger");
    
    app.Run();
    
    Log.Information("Application shut down gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
