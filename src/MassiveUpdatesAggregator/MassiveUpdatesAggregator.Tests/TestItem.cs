namespace MassiveUpdatesAggregator.Tests;

/// <summary>
/// Test object to for aggregation tests.
/// </summary>
#pragma warning disable CA1515 // Consider making public types internal. Need to be public for Moq
public sealed record TestItem : IAggregatorItem<object>
#pragma warning restore CA1515 // Consider making public types internal
{
    /// <summary>
    /// Aggregation key.
    /// </summary>
    public object Key => string.Empty;
}
