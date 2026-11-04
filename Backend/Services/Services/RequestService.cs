using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTOs;
using Common;
using Repositories;
using Services.IServices;

namespace Services.Services
{
    public class RequestService : IRequestService
    {
        private readonly IRequestRepository _repo;
        public RequestService(IRequestRepository repo) => _repo = repo;

        public async Task<PagedResult<RequestDto>> GetPagedAsync(RequestQuery query, CancellationToken ct)
        {
            var (items, total) = await _repo.GetPagedAsync(query, ct);
            return new PagedResult<RequestDto>(
                items.Select(r => r.ToDto()).ToList(), query.Page, query.PageSize, total);
        }

        public Task<UpdateStatusResult> UpdateStatusAsync(int id, UpdateStatusRequest request, CancellationToken ct)
        {
            
            if (!request.TryGetRowVersion(out var rowVersion))
                throw new ArgumentException("Version is not a valid row version.", nameof(request));

            return _repo.UpdateStatusAsync(id, request.NewStatus, rowVersion, request.ChangedBy, ct);
        }
        public Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(int id, CancellationToken ct)
            => _repo.GetHistoryAsync(id, ct);
        public async Task<BulkUpdateResult> BulkUpdateStatusAsync(
    BulkUpdateStatusRequest request, CancellationToken ct)
        {
            var results = new List<BulkItemResult>(request.Items.Count);

            foreach (var item in request.Items)
            {
                ct.ThrowIfCancellationRequested();

                if (!item.TryGetRowVersion(out var rowVersion))
                    throw new ArgumentException($"Invalid version for item {item.Id}.", nameof(request));

                var r = await _repo.UpdateStatusAsync(
                    item.Id, request.NewStatus, rowVersion, request.ChangedBy, ct);

                results.Add(new BulkItemResult(item.Id, r.Outcome, r.Request, r.Error));
            }

            var succeeded = results.Count(x => x.Outcome == UpdateOutcome.Success);
            return new BulkUpdateResult(results.Count, succeeded, results.Count - succeeded, results);
        }

    }
}
