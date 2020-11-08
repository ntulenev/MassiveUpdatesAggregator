using System;
using System.Collections.Generic;
using System.Text;

namespace MassiveUpdatesAggregator
{
    public interface IAggregationStrategy<Item, KeyType> where Item : IAggregatorItem<KeyType>
    {
        public Item Merge(IEnumerable<Item> items);
    }
}
