using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ShelfLocationsConsumer>();
    x.AddConsumer<StartRunConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "RackCalculation OK");
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

class ShelfLocationsConsumer : IConsumer<ShelfLocationsAssigned>
{
    public async Task Consume(ConsumeContext<ShelfLocationsAssigned> ctx)
    {
        var p = RunParamsStore.Map.GetValueOrDefault(ctx.Message.RunId, new RunParams(true, 50, 100, 200, true, 4, 5, 20));
        int skus = ctx.Message.Demand.Count;
        int maxSkusPerRack = Math.Max(1, p.MaxSkusPerRack);
        int rackCount = (int)Math.Ceiling((double)skus / maxSkusPerRack);

        var racks = new List<Rack>();
        for (int i=1;i<=rackCount;i++)
        {
            racks.Add(new Rack($"R{i}", p.RacksLevels, p.SlotsPerLevel, 1000));
        }

        var finalized = new List<ShelfLocation>();
        int rackIdx = 0, slotIdx = 0, totalSlots = p.RacksLevels * p.SlotsPerLevel;
        foreach (var loc in ctx.Message.Locations.OrderBy(l=>l.Rank))
        {
            if (slotIdx >= totalSlots) { slotIdx = 0; rackIdx++; }
            string rackId = racks[rackIdx].RackId;
            string slotId = $"L{(slotIdx / p.SlotsPerLevel)+1}-S{(slotIdx % p.SlotsPerLevel)+1}";
            slotIdx++;
            finalized.Add(loc with { RackId = rackId, SlotId = slotId });
        }

        await ctx.Publish(new RackLayoutCalculated(ctx.Message.RunId, racks, finalized, ctx.Message.Demand));
    }
}
