namespace MassiveUpdatesAggregator
{
    /// <summary>
    /// Interface that should be implemented for data items to use aggregator.
    /// </summary>
    public interface IAggregatorItem<KeyType>
    {
        public KeyType Key { get; }
    }
}
