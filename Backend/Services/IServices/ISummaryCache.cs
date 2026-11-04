using Common.DTOs;

namespace Services.IServices;

public interface ISummaryCache
{
    Task<RequestSummaryDto> GetOrCreateAsync(
        Func<CancellationToken, Task<RequestSummaryDto>> factory, CancellationToken ct);

    void Invalidate();
}