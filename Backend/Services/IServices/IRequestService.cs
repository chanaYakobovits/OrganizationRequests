using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DTOs;

namespace Services.IServices
{
    public interface IRequestService
    {
        Task<PagedResult<RequestDto>> GetPagedAsync(RequestQuery query, CancellationToken ct);

        Task<UpdateStatusResult> UpdateStatusAsync(int id, UpdateStatusRequest request, CancellationToken ct);
        Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(int id, CancellationToken ct);
    }
}
