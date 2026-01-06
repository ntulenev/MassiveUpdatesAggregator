using System.Threading.Channels;

using Nito.AsyncEx;

namespace MassiveUpdatesAggregator;

/// <summary>
/// Class that aggregates items per Key.
/// </summary>
/// <typeparam name="TItem">Type of the item.</typeparam>
/// <typeparam name="TKeyType">Type of the item key.</typeparam>
public sealed class Aggregator<TItem, TKeyType> :
            IAsyncEnumerable<TItem>
            where TItem : IAggregatorItem<TKeyType>
            where TKeyType : notnull
{
    /// <summary>
    /// Creates aggregator
    /// </summary>
    /// <param name="initialSize">Approximate size of the number of keys.</param>
    /// <param name="delay">Delay when gets aggregates data from items.</param>
    /// <param name="strategy">Aggregation strategy <see cref="IAggregationStrategy{Item, KeyType}"/>.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="initialSize"/> 
    /// or <paramref name="delay"/> is incorrect.</exception>
    public Aggregator(int initialSize,
                      TimeSpan delay,
                      IAggregationStrategy<TItem, TKeyType> strategy,
                      CancellationToken ct = default)
    {
        if (initialSize <= 0)
        {
            throw new ArgumentException("InitialSize should be more than zero.", nameof(initialSize));
        }

        if (delay == TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be more that zero.", nameof(delay));
        }

        ArgumentNullException.ThrowIfNull(strategy);

        _items = new Dictionary<TKeyType, LinkedList<TItem>>(initialSize);
        _scheduledWork = new Dictionary<TKeyType, Task>(initialSize);
        _ct = ct;
        _delay = delay;
        _channel = Channel.CreateUnbounded<IEnumerable<TItem>>();
        _strategy = strategy;
    }

    /// <summary>
    /// Checks when aggregator is activated
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Add <typeparamref name="TItem"/> in aggregator.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when 
    /// <typeparamref name="TItem"/> is null.</exception>
    public async Task SendAsync(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var key = item.Key;

        using (await _guard.LockAsync(_ct).ConfigureAwait(false))
        {
            if (!_isRunning)
            {
                return;
            }

            if (!_items.TryGetValue(key, out var list))
            {
                list = new LinkedList<TItem>();
                _items.Add(key, list);
                _ = list.AddLast(item);
                _scheduledWork.Add(key, CreateAggregationTaskAsync(key));
            }
            else
            {
                _ = list.AddLast(item);
            }
        }
    }

    /// <summary>
    /// Creates async iterator for aggregated items.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Messages async iterator.</returns>
    public async IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var items))
            {
                yield return _strategy.Merge(items);
            }
        }
    }

    /// <summary>
    /// Stops aggregator.
    /// </summary>
    public async Task StopAsync()
    {
        IEnumerable<Task> tasks;

        using (await _guard.LockAsync(_ct).ConfigureAwait(false))
        {
            if (!_isRunning)
            {
                throw new InvalidOperationException("Aggregator is already in stopping state.");
            }

            _isRunning = false;

            tasks = [.. _scheduledWork.Select(x => x.Value)];
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task CreateAggregationTaskAsync(TKeyType key)
    {
        await Task.Delay(_delay, _ct).ConfigureAwait(false);

        LinkedList<TItem>? list;

        using (await _guard.LockAsync(_ct).ConfigureAwait(false))
        {
            if (!_items.TryGetValue(key, out list))
            {
                throw new InvalidOperationException(
                        $"List with data for key {key} not found");
            }

            _ = _items.Remove(key);
            _ = _scheduledWork.Remove(key);

        }

        await _channel.Writer.WriteAsync(list, _ct).ConfigureAwait(false);
    }

    private readonly Dictionary<TKeyType, Task> _scheduledWork;
    private readonly Dictionary<TKeyType, LinkedList<TItem>> _items;
    private readonly AsyncLock _guard = new();
    private readonly CancellationToken _ct;
    private readonly Channel<IEnumerable<TItem>> _channel;
    private readonly IAggregationStrategy<TItem, TKeyType> _strategy;
    private volatile bool _isRunning = true;
    private readonly TimeSpan _delay;
}
