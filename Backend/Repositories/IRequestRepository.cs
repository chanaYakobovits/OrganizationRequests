using Common.DTOs;
using Common.Models;

namespace Repositories;

public interface IRequestRepository
{
    Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
        RequestQuery query, CancellationToken ct);
}