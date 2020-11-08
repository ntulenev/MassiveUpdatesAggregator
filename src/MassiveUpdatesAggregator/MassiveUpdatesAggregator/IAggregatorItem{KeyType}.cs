namespace MassiveUpdatesAggregator
{
    public interface IAggregatorItem<KeyType>
    {
        public KeyType Key { get; }
    }
}
