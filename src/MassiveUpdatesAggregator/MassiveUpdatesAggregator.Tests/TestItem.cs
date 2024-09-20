namespace MassiveUpdatesAggregator.Tests;

/// <summary>
/// Test object to for aggregation tests.
/// </summary>
public sealed record TestItem : IAggregatorItem<object>
{
    /// <summary>
    /// Aggregation key.
    /// </summary>
    public object Key => string.Empty;
}
