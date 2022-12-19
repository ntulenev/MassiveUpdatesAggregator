namespace MassiveUpdatesAggregator.Tests;

public record TestItem : IAggregatorItem<object>
{
    public object Key => string.Empty;
}
