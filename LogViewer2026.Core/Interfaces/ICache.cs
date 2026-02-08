namespace LogViewer2026.Core.Interfaces;

public interface ICache<TKey, TValue> : IDisposable where TKey : notnull
{
    bool TryGet(TKey key, out TValue? value);
    void Add(TKey key, TValue value);
    void Clear();
    int Count { get; }
}
