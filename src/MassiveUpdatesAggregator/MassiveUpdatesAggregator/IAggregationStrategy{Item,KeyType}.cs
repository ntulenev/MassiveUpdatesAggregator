using System.Collections.Generic;

namespace MassiveUpdatesAggregator
{
    /// <summary>
    /// Interface that presents logic for aggregation strategy.
    /// </summary>
    public interface IAggregationStrategy<Item, KeyType> where Item : IAggregatorItem<KeyType>
    {
        public Item Merge(IEnumerable<Item> items);
    }
}
