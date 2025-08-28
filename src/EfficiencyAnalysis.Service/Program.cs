using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StationsAllocatedConsumer>();
    x.AddConsumer<BatchesCreatedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "EfficiencyAnalysis OK");
app.Run();

static class BatchStore
{
    public static readonly Dictionary<Guid, List<Batch>> Batches = new();
}

class BatchesCreatedConsumer : IConsumer<BatchesCreated>
{
    public Task Consume(ConsumeContext<BatchesCreated> ctx)
    {
        BatchStore.Batches[ctx.Message.RunId] = ctx.Message.Batches;
        return Task.CompletedTask;
    }
}

class StationsAllocatedConsumer : IConsumer<StationsAllocated>
{
    public async Task Consume(ConsumeContext<StationsAllocated> ctx)
    {
        var batches = BatchStore.Batches.GetValueOrDefault(ctx.Message.RunId, new List<Batch>());
        int items = 0;
        int presentations = 0;
        var byRackItems = new Dictionary<string, int>();
        foreach (var b in batches)
        {
            string? lastRack = null;
            foreach (var line in b.Lines)
            {
                items += line.Qty;
                if (!byRackItems.ContainsKey(line.RackId)) byRackItems[line.RackId] = 0;
                byRackItems[line.RackId] += line.Qty;

                if (lastRack == null || lastRack != line.RackId) { presentations++; lastRack = line.RackId; }
            }
        }
        double hitRate = presentations == 0 ? 0 : (double)items / presentations;
        var byRackAvg = byRackItems.ToDictionary(kv => kv.Key, kv => (double)kv.Value / Math.Max(1, batches.Count));
        var result = new HitRateResult(batches.FirstOrDefault()?.Mode ?? "PTO", hitRate, items, presentations, byRackAvg);
        await ctx.Publish(new HitRateCalculated(ctx.Message.RunId, result));
    }
}
