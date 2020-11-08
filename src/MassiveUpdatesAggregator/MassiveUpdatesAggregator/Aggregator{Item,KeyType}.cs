using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Nito.AsyncEx;

namespace MassiveUpdatesAggregator
{
    public class Aggregator<Item, KeyType> : IAsyncEnumerable<Item> where Item : IAggregatorItem<KeyType>
    {
        public Aggregator(int initialSize, int millisecondsDelay, IAggregationStrategy<Item, KeyType> strategy, CancellationToken ct = default)
        {
            if (initialSize <= 0)
                throw new ArgumentException("initialSize should be more than zero.", nameof(initialSize));

            if (millisecondsDelay <= 0)
                throw new ArgumentException("delay can not be negative.", nameof(millisecondsDelay));

            if (strategy is null)
                throw new ArgumentNullException(nameof(strategy));

            _items = new Dictionary<KeyType, LinkedList<Item>>(initialSize);
            _scheduledWork = new Dictionary<KeyType, Task>(initialSize);
            _ct = ct;
            _delay = millisecondsDelay;
            _channel = Channel.CreateUnbounded<IEnumerable<Item>>();
            _strategy = strategy;
        }

        public bool IsRunning => _isRunning;

        public async Task SendAsync(Item item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var key = item.Key;

            using (await _guard.LockAsync(_ct).ConfigureAwait(false))
            {
                if (_isRunning is false)
                    return;

                if (!_items.TryGetValue(key, out LinkedList<Item> list))
                {
                    list = new LinkedList<Item>();
                    _items.Add(key, list);
                    list.AddLast(item);
                    _scheduledWork.Add(key, Aggregation(key));
                }
                else
                {
                    list.AddLast(item);
                }
            }
        }

        public async IAsyncEnumerator<Item> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out IEnumerable<Item> items))
                {
                    yield return _strategy.Merge(items);
                }
            }
        }

        public async Task StopAsync()
        {
            IEnumerable<Task> tasks;

            using (await _guard.LockAsync(_ct).ConfigureAwait(false))
            {
                if (_isRunning is false)
                    throw new InvalidOperationException("Application is already in stopping state.");
                _isRunning = false;

                tasks = _scheduledWork.Select(x => x.Value).ToList();
            }

            await Task.WhenAll(tasks);
        }

        private async Task Aggregation(KeyType key)
        {
            await Task.Delay(_delay, _ct).ConfigureAwait(false);

            LinkedList<Item> list;

            using (await _guard.LockAsync(_ct).ConfigureAwait(false))
            {
                _ = _items.TryGetValue(key, out list);

                _items.Remove(key);
                _scheduledWork.Remove(key);

            }

            if (list is null)
                throw new InvalidOperationException($"List with data for key {key} not found");

            await _channel.Writer.WriteAsync(list, _ct).ConfigureAwait(false);
        }

        private readonly Dictionary<KeyType, Task> _scheduledWork;

        private readonly Dictionary<KeyType, LinkedList<Item>> _items;

        private readonly AsyncLock _guard = new AsyncLock();

        private readonly CancellationToken _ct;

        private readonly Channel<IEnumerable<Item>> _channel;

        private readonly IAggregationStrategy<Item, KeyType> _strategy;

        private volatile bool _isRunning = true;

        private readonly int _delay;
    }
}
