using Contracts;
using MassTransit;
using CsvHelper;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StartRunConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "rabbitmq", "/", h => { });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();
app.MapGet("/", () => "SalesDataAnalysis OK");
app.Run();

class StartRunConsumer : IConsumer<StartRunCommand>
{
    public async Task Consume(ConsumeContext<StartRunCommand> ctx)
    {
        var path = ctx.Message.DatasetPath;
        var demand = new Dictionary<string, SkuDemand>();
        var catBySku = new Dictionary<string, string>();
        var monthQty = new Dictionary<string, int[]>();

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>();
        foreach (var r in records)
        {
            string sku = ((string)r.Product).Trim();
            string cat = ((string)r.ProductCategory).Trim();
            int qty = int.Parse((string)r.Qty);
            catBySku[sku] = cat;
            if (!demand.TryGetValue(sku, out var d))
            {
                d = new SkuDemand(sku, 0, 0, 0, false, cat);
                demand[sku] = d;
                monthQty[sku] = new int[12];
            }
            demand[sku] = d with { TotalUnits = d.TotalUnits + qty, OrderCount = d.OrderCount + 1 };

            string dateStr = (string)r.OrderDate;
            DateTime dt;
            if (!DateTime.TryParse(dateStr, out dt))
            {
                dt = DateTime.ParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            monthQty[sku][dt.Month - 1] += qty;
        }

        var demandList = new List<SkuDemand>();
        foreach (var kv in demand)
        {
            var d = kv.Value;
            var months = monthQty[kv.Key];
            var total = months.Sum();
            bool seasonal = total > 0 && months.Max() >= total * 0.3;
            double velocity = d.OrderCount == 0 ? 0 : (double)d.TotalUnits / Math.Max(1, d.OrderCount);
            demandList.Add(d with { Velocity = velocity, Seasonal = seasonal, Category = catBySku[kv.Key] });
        }

        await ctx.Publish(new SalesPatternsIdentified(ctx.Message.RunId, demandList));
    }
}
