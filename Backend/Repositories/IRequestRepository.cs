using Common.DTOs;
using Common.Models;

namespace Repositories;

public interface IRequestRepository
{
    Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
        RequestQuery query, CancellationToken ct);
 
    Task<UpdateStatusResult> UpdateStatusAsync(
        int id, RequestStatus newStatus, byte[] expectedVersion, string changedBy, CancellationToken ct);
    Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(int id, CancellationToken ct);
    Task<RequestSummaryDto> GetSummaryAsync(CancellationToken ct);
}