using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace WarningSystems.Core.Caching;

public sealed class IssueWizardCache(IMemoryCache memory)
{
    private long _issueRevision;
    private long _referenceRevision;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    public void Invalidate(bool includeReferenceData)
    {
        Interlocked.Increment(ref _issueRevision);
        if (includeReferenceData)
            Interlocked.Increment(ref _referenceRevision);
    }

    public async Task<T> GetAsync<T>(string userId, string name, bool referenceData, Func<Task<T>> load)
    {
        var revision = referenceData
            ? Interlocked.Read(ref _referenceRevision)
            : Interlocked.Read(ref _issueRevision);
        var key = $"issue-wizard:{userId}:{name}:{revision}";
        if (!memory.TryGetValue(key, out string? json) || json is null)
        {
            var value = await load();
            json = JsonSerializer.Serialize(value);
            memory.Set(key, json, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = Lifetime
            });
        }
        return JsonSerializer.Deserialize<T>(json)!;
    }
}
