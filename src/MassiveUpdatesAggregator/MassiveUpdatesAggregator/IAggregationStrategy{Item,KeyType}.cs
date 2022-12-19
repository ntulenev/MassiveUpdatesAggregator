namespace MassiveUpdatesAggregator;

/// <summary>
/// Interface that presents logic for aggregation strategy.
/// </summary>
public interface IAggregationStrategy<Item, KeyType> where Item : IAggregatorItem<KeyType>
{
    /// <summary>
    /// Merges aggregates items in single.
    /// </summary>
    /// <param name="items">Items to aggregate.</param>
    /// <returns>Aggregates item.</returns>
    public Item Merge(IEnumerable<Item> items);
}
