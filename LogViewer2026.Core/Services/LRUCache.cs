using LogViewer2026.Core.Interfaces;

namespace LogViewer2026.Core.Services;

public sealed class LRUCache<TKey, TValue> : ICache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    public LRUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = new LinkedList<CacheItem>();
    }

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool TryGet(TKey key, out TValue? value)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lock.EnterWriteLock();
                try
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public void Add(TKey key, TValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _lruList.Remove(existingNode);
                existingNode.Value.Value = value;
                _lruList.AddFirst(existingNode);
                return;
            }

            if (_cache.Count >= _capacity)
            {
                var lru = _lruList.Last;
                if (lru != null)
                {
                    _lruList.RemoveLast();
                    _cache.Remove(lru.Value.Key);
                }
            }

            var cacheItem = new CacheItem { Key = key, Value = value };
            var node = new LinkedListNode<CacheItem>(cacheItem);
            _lruList.AddFirst(node);
            _cache[key] = node;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
            _lruList.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _lock?.Dispose();
    }

    private sealed class CacheItem
    {
        public required TKey Key { get; init; }
        public TValue Value { get; set; } = default!;
    }
}
