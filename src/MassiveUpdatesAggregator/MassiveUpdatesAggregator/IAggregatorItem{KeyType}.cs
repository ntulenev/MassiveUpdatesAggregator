namespace MassiveUpdatesAggregator;

/// <summary>
/// Interface that should be implemented 
/// for data items to use aggregator.
/// </summary>
public interface IAggregatorItem<TKeyType>
{
    /// <summary>
    /// Aggregation key.
    /// </summary>
    TKeyType Key { get; }
}
