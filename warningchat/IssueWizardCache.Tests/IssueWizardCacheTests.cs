using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using WarningSystems.Core.Caching;

namespace IssueWizardCache.Tests;

public class IssueWizardCacheTests
{
    [Test]
    public async Task CacheIsIsolatedByUserAndWarningAndReturnsIndependentSnapshots()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new IssueWizardCache(memory);
        var loads = 0;
        Task<List<string>> Load() => Task.FromResult(new List<string> { $"load-{++loads}" });
        var first = await cache.GetAsync("alice", "warning:1", false, Load);
        first.Add("changed by caller");
        var second = await cache.GetAsync("alice", "warning:1", false, Load);
        Assert.That(second, Has.Count.EqualTo(1));
        Assert.That(loads, Is.EqualTo(1));
        await cache.GetAsync("bob", "warning:1", false, Load);
        await cache.GetAsync("alice", "warning:2", false, Load);
        Assert.That(loads, Is.EqualTo(3));
    }

    [Test]
    public async Task IssueWriteRefreshesIssuesForAllUsersButKeepsReferenceData()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new IssueWizardCache(memory);
        var value = 1;
        Task<int> Load() => Task.FromResult(value);
        await cache.GetAsync("alice", "warning:1", false, Load);
        await cache.GetAsync("bob", "warning:1", false, Load);
        await cache.GetAsync("alice", "employees", true, Load);
        value = 2;
        cache.Invalidate(false);
        Assert.That(await cache.GetAsync("alice", "warning:1", false, Load), Is.EqualTo(2));
        Assert.That(await cache.GetAsync("bob", "warning:1", false, Load), Is.EqualTo(2));
        Assert.That(await cache.GetAsync("alice", "employees", true, Load), Is.EqualTo(1));
        cache.Invalidate(true);
        Assert.That(await cache.GetAsync("alice", "employees", true, Load), Is.EqualTo(2));
    }

    [Test]
    public async Task ReadStartedBeforeWriteCannotRepopulateCurrentRevision()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new IssueWizardCache(memory);
        var pending = new TaskCompletionSource<int>();
        var oldRead = cache.GetAsync("alice", "warning:1", false, () => pending.Task);
        cache.Invalidate(false);
        Assert.That(await cache.GetAsync("alice", "warning:1", false, () => Task.FromResult(2)), Is.EqualTo(2));
        pending.SetResult(1);
        await oldRead;
        Assert.That(await cache.GetAsync("alice", "warning:1", false, () => Task.FromResult(3)), Is.EqualTo(2));
    }

    [Test]
    public async Task FailedLoadIsRetriedAndEvictionLoadsDatabaseAgain()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new IssueWizardCache(memory);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await cache.GetAsync<int>("alice", "warning:1", false,
                () => Task.FromException<int>(new InvalidOperationException())));
        Assert.That(await cache.GetAsync("alice", "warning:1", false, () => Task.FromResult(1)), Is.EqualTo(1));
        memory.Compact(1);
        Assert.That(await cache.GetAsync("alice", "warning:1", false, () => Task.FromResult(2)), Is.EqualTo(2));
    }
}
