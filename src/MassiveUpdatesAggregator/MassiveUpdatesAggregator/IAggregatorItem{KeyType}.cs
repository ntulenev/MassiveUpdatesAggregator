namespace MassiveUpdatesAggregator;

/// <summary>
/// Interface that should be implemented for data items to use aggregator.
/// </summary>
public interface IAggregatorItem<KeyType>
{
    /// <summary>
    /// Aggregation key.
    /// </summary>
    public KeyType Key { get; }
}
