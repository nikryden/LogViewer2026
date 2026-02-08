using FluentAssertions;
using LogViewer2026.Core.Services;

namespace LogViewer2026.Core.Tests.Services;

public class LRUCacheTests
{
    [Fact]
    public void Add_ShouldStoreValue()
    {
        var cache = new LRUCache<int, string>(capacity: 10);
        
        cache.Add(1, "value1");
        
        cache.TryGet(1, out var value).Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void TryGet_WithNonExistentKey_ShouldReturnFalse()
    {
        var cache = new LRUCache<int, string>(capacity: 10);
        
        var result = cache.TryGet(999, out var value);
        
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void Add_WhenCapacityExceeded_ShouldEvictLeastRecentlyUsed()
    {
        var cache = new LRUCache<int, string>(capacity: 2);
        
        cache.Add(1, "value1");
        cache.Add(2, "value2");
        cache.Add(3, "value3");
        
        cache.TryGet(1, out _).Should().BeFalse();
        cache.TryGet(2, out _).Should().BeTrue();
        cache.TryGet(3, out _).Should().BeTrue();
    }

    [Fact]
    public void TryGet_ShouldUpdateAccessOrder()
    {
        var cache = new LRUCache<int, string>(capacity: 2);
        
        cache.Add(1, "value1");
        cache.Add(2, "value2");
        cache.TryGet(1, out _);
        cache.Add(3, "value3");
        
        cache.TryGet(1, out _).Should().BeTrue();
        cache.TryGet(2, out _).Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        var cache = new LRUCache<int, string>(capacity: 10);
        
        cache.Add(1, "value1");
        cache.Add(2, "value2");
        cache.Clear();
        
        cache.Count.Should().Be(0);
        cache.TryGet(1, out _).Should().BeFalse();
    }

    [Fact]
    public void Count_ShouldReflectNumberOfEntries()
    {
        var cache = new LRUCache<int, string>(capacity: 10);
        
        cache.Count.Should().Be(0);
        cache.Add(1, "value1");
        cache.Count.Should().Be(1);
        cache.Add(2, "value2");
        cache.Count.Should().Be(2);
    }
}
