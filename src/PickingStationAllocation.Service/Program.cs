using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BatchesCreatedConsumer>();
    x.AddConsumer<StartRunConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "PickingStationAllocation OK");
app.Run();

static class RunParamsStore
{
    public static readonly Dictionary<Guid, RunParams> Map = new();
}
class StartRunConsumer : IConsumer<StartRunCommand>
{
    public Task Consume(ConsumeContext<StartRunCommand> ctx)
    {
        RunParamsStore.Map[ctx.Message.RunId] = ctx.Message.Params;
        return Task.CompletedTask;
    }
}

class BatchesCreatedConsumer : IConsumer<BatchesCreated>
{
    public async Task Consume(ConsumeContext<BatchesCreated> ctx)
    {
        var p = RunParamsStore.Map.GetValueOrDefault(ctx.Message.RunId, new RunParams(true, 50, 100, 200, true, 4, 5, 20));
        int stations = Math.Max(1, p.MaxStationsOpen);
        var assignments = Enumerable.Range(1, stations).Select(i => new StationAssignment($"S{i}", new List<string>())).ToList();
        int s = 0;
        foreach (var b in ctx.Message.Batches)
        {
            assignments[s].BatchIds.Add(b.BatchId);
            s = (s + 1) % stations;
        }
        await ctx.Publish(new StationsAllocated(ctx.Message.RunId, assignments));
    }
}
