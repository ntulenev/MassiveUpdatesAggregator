namespace MassiveUpdatesAggregator;

/// <summary>
/// Interface that presents logic for aggregation strategy.
/// </summary>
public interface IAggregationStrategy<TItem, TKeyType>
                 where TItem : IAggregatorItem<TKeyType>
{
    /// <summary>
    /// Merges aggregates items in single.
    /// </summary>
    /// <param name="items">Items to aggregate.</param>
    /// <returns>Aggregates item.</returns>
    TItem Merge(IEnumerable<TItem> items);
}
