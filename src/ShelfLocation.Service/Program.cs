using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SkuGroupsConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "ShelfLocation OK");
app.Run();

class SkuGroupsConsumer : IConsumer<SkuGroupsCreated>
{
    public async Task Consume(ConsumeContext<SkuGroupsCreated> ctx)
    {
        var ranked = ctx.Message.Demand.OrderByDescending(d => d.Velocity).ToList();
        var locations = new List<ShelfLocation>();
        int rank = 1;
        foreach (var d in ranked)
        {
            locations.Add(new ShelfLocation(d.SkuId, "RACK_TBD", $"SLOT_{rank}", rank++));
        }
        await ctx.Publish(new ShelfLocationsAssigned(ctx.Message.RunId, locations, ctx.Message.Demand));
    }
}
