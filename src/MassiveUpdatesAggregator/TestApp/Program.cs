using MassiveUpdatesAggregator;

var testData = "The quick brown fox jumps over the lazy dog".Split(" ");

using var cts = new CancellationTokenSource();
var rnd = new Random();

// Creation of the aggregator class
var aggregator = new Aggregator<TestData, int>(
                            initialSize: 100,
                            delay: TimeSpan.FromSeconds(1),
                            new SumStrategy(),
                            cts.Token);

bool isWork = true;

int i = 0;

_ = Task.Run(async () =>
        {
            while (isWork)
            {
                await Task.Delay(100 * rnd.Next(1, 7));

                // Send data for key = 1  
                await aggregator.SendAsync(new TestData(
                                                   Key: 1,
                                                   Value: testData[i++]));

                if (i == testData.Length)
                {
                    i = 0;
                }
            }
        });

_ = Task.Run(async () =>
{
    // Iterate aggregated items
    await foreach (var item in aggregator.WithCancellation(cts.Token))
    {
        Console.WriteLine($"{item.Key} {item.Value}");
    }
});

Console.ReadKey();
cts.Cancel();
isWork = false;

try
{
    // Stop aggregator
    await aggregator.StopAsync();
}
catch (TaskCanceledException)
{

}

Console.WriteLine("All done.");

/// <summary>
/// This class represents data for aggregation.
/// </summary>
/// <param name="Key">Aggregation key.</param>
/// <param name="Value">Aggregation value.</param>
public sealed record TestData(int Key, string Value) : IAggregatorItem<int>;

/// <summary>
/// This class implements aggregation strategy for bunch of data.
/// </summary>
public sealed class SumStrategy : IAggregationStrategy<TestData, int>
{
    public TestData Merge(IEnumerable<TestData> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // Simple strategy - just create new item with key and contact of all values
        var key = items.First().Key;
        return new TestData(
            Key: key,
            Value: string.Join("-", items.Select(x => x.Value)));
    }
}
