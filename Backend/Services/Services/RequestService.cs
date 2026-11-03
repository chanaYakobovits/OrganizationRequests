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
    }
}
