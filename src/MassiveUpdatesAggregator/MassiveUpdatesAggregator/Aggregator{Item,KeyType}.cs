using System.Threading.Channels;

using Nito.AsyncEx;

namespace MassiveUpdatesAggregator;

/// <summary>
/// Class that aggregates items per Key.
/// </summary>
/// <typeparam name="Item">Type of the item.</typeparam>
/// <typeparam name="KeyType">Type of the item key.</typeparam>
public class Aggregator<Item, KeyType> : IAsyncEnumerable<Item> where Item : IAggregatorItem<KeyType>
                                                                where KeyType : notnull
{
    /// <summary>
    /// Creates aggregator
    /// </summary>
    /// <param name="initialSize">Approximate size of the number of keys.</param>
    /// <param name="delay">Delay when gets aggregates data from items.</param>
    /// <param name="strategy">Aggregation strategy <see cref="IAggregationStrategy"/>.</param>
    /// <param name="ct">Cancellation token for async operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="initialSize"/> or <paramref name="millisecondsDelay"/> is incorrect.</exception>
    public Aggregator(int initialSize, TimeSpan delay, IAggregationStrategy<Item, KeyType> strategy, CancellationToken ct = default)
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

        _items = new Dictionary<KeyType, LinkedList<Item>>(initialSize);
        _scheduledWork = new Dictionary<KeyType, Task>(initialSize);
        _ct = ct;
        _delay = delay;
        _channel = Channel.CreateUnbounded<IEnumerable<Item>>();
        _strategy = strategy;
    }

    /// <summary>
    /// Checks when aggregator is activated
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Add <typeparamref name="Item"/> in aggregator.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <typeparamref name="Item"/> is null.</exception>
    public async Task SendAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var key = item.Key;

        using (await _guard.LockAsync(_ct).ConfigureAwait(false))
        {
            if (_isRunning is false)
                return;

            if (!_items.TryGetValue(key, out LinkedList<Item>? list))
            {
                list = new LinkedList<Item>();
                _items.Add(key, list);
                list.AddLast(item);
                _scheduledWork.Add(key, CreateAggregationTask(key));
            }
            else
            {
                list.AddLast(item);
            }
        }
    }

    /// <summary>
    /// Creates async iterator for aggregated items.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>Messages async iterator.</returns>
    public async IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out IEnumerable<Item>? items))
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
            if (_isRunning is false)
            {
                throw new InvalidOperationException("Aggregator is already in stopping state.");
            }

            _isRunning = false;

            tasks = _scheduledWork.Select(x => x.Value).ToList();
        }

        await Task.WhenAll(tasks);
    }

    private async Task CreateAggregationTask(KeyType key)
    {
        await Task.Delay(_delay, _ct).ConfigureAwait(false);

        LinkedList<Item>? list;

        using (await _guard.LockAsync(_ct).ConfigureAwait(false))
        {
            if (!_items.TryGetValue(key, out list))
            {
                throw new InvalidOperationException($"List with data for key {key} not found");
            }

            _items.Remove(key);
            _scheduledWork.Remove(key);

        }

        await _channel.Writer.WriteAsync(list, _ct).ConfigureAwait(false);
    }

    private readonly Dictionary<KeyType, Task> _scheduledWork;
    private readonly Dictionary<KeyType, LinkedList<Item>> _items;
    private readonly AsyncLock _guard = new();
    private readonly CancellationToken _ct;
    private readonly Channel<IEnumerable<Item>> _channel;
    private readonly IAggregationStrategy<Item, KeyType> _strategy;
    private volatile bool _isRunning = true;
    private readonly TimeSpan _delay;
}
