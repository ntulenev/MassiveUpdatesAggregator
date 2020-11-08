using System.Collections.Generic;

namespace MassiveUpdatesAggregator
{
    public interface IAggregationStrategy<Item, KeyType> where Item : IAggregatorItem<KeyType>
    {
        public Item Merge(IEnumerable<Item> items);
    }
}
