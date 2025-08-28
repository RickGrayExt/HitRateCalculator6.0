using Contracts;
using MassTransit;

public class StartRunConsumer : IConsumer<StartRunCommand>
{
    public async Task Consume(ConsumeContext<StartRunCommand> context)
    {
        var cmd = context.Message;

        // Example calculation (replace with real logic)
        var dataset = System.IO.File.ReadAllLines(cmd.DatasetPath).Skip(1);
        var total = dataset.Count();
        var hitRate = total > 0 ? (double)new Random().Next(50, 100) / 100 : 0.0;

        Console.WriteLine($"✅ Run {cmd.RunId} calculated. Hit Rate = {hitRate:P2}");

        await context.Publish(new HitRateCalculated
        {
            RunId = cmd.RunId,
            HitRate = hitRate
        });
    }
}
