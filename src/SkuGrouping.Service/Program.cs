using Contracts;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SalesPatternsConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "SkuGrouping OK");
app.Run();

class SalesPatternsConsumer : IConsumer<SalesPatternsIdentified>
{
    public async Task Consume(ConsumeContext<SalesPatternsIdentified> ctx)
    {
        var byCat = ctx.Message.Demand.GroupBy(d => d.Category);
        var groups = new List<SkuGroup>();
        foreach (var g in byCat)
        {
            var list = g.OrderByDescending(x => x.Velocity).ToList();
            double median = list.Count == 0 ? 0 : list[list.Count/2].Velocity;
            var fast = list.Where(x => x.Velocity >= median).Select(x => x.SkuId).ToList();
            var slow = list.Where(x => x.Velocity < median).Select(x => x.SkuId).ToList();
            if (fast.Count > 0) groups.Add(new SkuGroup($"{g.Key}-FAST", fast));
            if (slow.Count > 0) groups.Add(new SkuGroup($"{g.Key}-SLOW", slow));
        }
        await ctx.Publish(new SkuGroupsCreated(ctx.Message.RunId, groups, ctx.Message.Demand));
    }
}
