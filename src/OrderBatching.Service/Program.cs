using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RackLayoutConsumer>();
    x.AddConsumer<StartRunConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "OrderBatching OK");
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

class RackLayoutConsumer : IConsumer<RackLayoutCalculated>
{
    public async Task Consume(ConsumeContext<RackLayoutCalculated> ctx)
    {
        var p = RunParamsStore.Map.GetValueOrDefault(ctx.Message.RunId, new RunParams(true, 50, 100, 200, true, 4, 5, 20));
        var orders = new List<OrderLine>();
        var rackBySku = ctx.Message.Locations.ToDictionary(l => l.SkuId, l => l.RackId);
        foreach (var d in ctx.Message.Demand)
        {
            int qty = Math.Max(1, (int)Math.Round(d.Velocity));
            orders.Add(new OrderLine(Guid.NewGuid().ToString(), d.SkuId, qty, rackBySku[d.SkuId]));
        }

        var batches = new List<Batch>();
        int idx = 0;
        while (idx < orders.Count)
        {
            var lines = orders.Skip(idx).Take(p.MaxBatchLines).ToList();
            idx += lines.Count;
            batches.Add(new Batch(Guid.NewGuid().ToString(), p.UsePto ? "PTO" : "PTL", lines));
        }

        await ctx.Publish(new BatchesCreated(ctx.Message.RunId, batches, p.UsePto ? "PTO" : "PTL"));
    }
}
