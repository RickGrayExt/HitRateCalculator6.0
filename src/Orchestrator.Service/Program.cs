using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Retry if RabbitMQ isn't ready
        cfg.UseCircuitBreaker(cb =>
        {
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.TripThreshold = 15;
            cb.ActiveThreshold = 10;
            cb.ResetInterval = TimeSpan.FromMinutes(5);
        });
    });
});

var app = builder.Build();
app.MapGet("/health", () => "ok");

app.MapPost("/run", async (StartRunCommand req, IBus bus) =>
{
    var dataset = string.IsNullOrWhiteSpace(req.DatasetPath) ? "/app/data/DataSetClean.csv" : req.DatasetPath;
    var runId = req.RunId == Guid.Empty ? Guid.NewGuid() : req.RunId;
    var cmd = req with { RunId = runId, DatasetPath = dataset };
    
    await bus.Publish(cmd);

    // ✅ Always return JSON with runId + status
    return Results.Json(new { runId, status = "submitted" });
});


app.Run("http://0.0.0.0:8080");
