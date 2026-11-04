using Common.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Services.IServices;

namespace Services.Services;

public class SummaryCache : ISummaryCache
{
    private const string Key = "requests:summary";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly IMemoryCache _cache;
    public SummaryCache(IMemoryCache cache) => _cache = cache;

    public async Task<RequestSummaryDto> GetOrCreateAsync(
        Func<CancellationToken, Task<RequestSummaryDto>> factory, CancellationToken ct)
    {
        if (_cache.TryGetValue(Key, out RequestSummaryDto? cached) && cached is not null)
            return cached;

        var value = await factory(ct);

        _cache.Set(Key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ttl
        });
        return value;
    }

    public void Invalidate() => _cache.Remove(Key);
}