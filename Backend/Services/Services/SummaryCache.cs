using Common.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Services.IServices;

namespace Services.Services;

public class SummaryCache : ISummaryCache
{
    private const string Key = "requests:summary";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly IMemoryCache _cache;
    private readonly object _sync = new();
    private long _generation;
    public SummaryCache(IMemoryCache cache) => _cache = cache;

    public async Task<RequestSummaryDto> GetOrCreateAsync(
        Func<CancellationToken, Task<RequestSummaryDto>> factory, CancellationToken ct)
    {
        if (_cache.TryGetValue(Key, out RequestSummaryDto? cached) && cached is not null)
            return cached;

        var generation = Interlocked.Read(ref _generation);
        var value = await factory(ct);

        lock (_sync)
        {
            
            if (generation == _generation)
                _cache.Set(Key, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = Ttl
                });
        }
        return value;
    }

    public void Invalidate()
    {
        lock (_sync)
        {
            _generation++;
            _cache.Remove(Key);
        }
    }
}