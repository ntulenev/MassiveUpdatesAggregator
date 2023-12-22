namespace MassiveUpdatesAggregator.Tests;

/// <summary>
/// Test object to for aggretation tests.
/// </summary>
public sealed record TestItem : IAggregatorItem<object>
{
    /// <summary>
    /// Aggregation key.
    /// </summary>
    public object Key => string.Empty;
}
