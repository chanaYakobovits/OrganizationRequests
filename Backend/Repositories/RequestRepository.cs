using Common;
using Common.DTOs;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repositories;

public class RequestRepository : IRequestRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<RequestRepository> _logger;

    public RequestRepository(AppDbContext db, ILogger<RequestRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Request> Items, int TotalCount)> GetPagedAsync(
        RequestQuery q, CancellationToken ct)
    {
        IQueryable<Request> query = _db.Requests.AsNoTracking();

        
        if (q.Status.HasValue) query = query.Where(r => r.Status == q.Status);
        if (q.Priority.HasValue) query = query.Where(r => r.Priority == q.Priority);
        if (q.OrganizationName != null) query = query.Where(r => r.OrganizationName == q.OrganizationName);
        if (q.AssignedTo != null) query = query.Where(r => r.AssignedTo == q.AssignedTo);
        if (q.CreatedFrom.HasValue) query = query.Where(r => r.CreatedAt >= q.CreatedFrom);
        if (q.CreatedTo.HasValue)
        {
            var to = q.CreatedTo.Value;
            if (to.TimeOfDay == TimeSpan.Zero)
            {
                var endExclusive = to.AddDays(1);
                query = query.Where(r => r.CreatedAt < endExclusive);
            }
            else
            {
                query = query.Where(r => r.CreatedAt <= to);
            }
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(r => r.Title.StartsWith(s) || r.OrganizationName.StartsWith(s));
        }

        var total = await query.CountAsync(ct);

       
        query = ApplySorting(query, q.SortBy, q.SortDir);


        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);
       
        return (items, total);
    }

    private static IQueryable<Request> ApplySorting(
        IQueryable<Request> query, RequestSortField field, SortDirection dir)
    {
        var asc = dir == SortDirection.Asc;
        var ordered = field switch
        {
            RequestSortField.UpdatedAt => asc ? query.OrderBy(r => r.UpdatedAt) : query.OrderByDescending(r => r.UpdatedAt),
            RequestSortField.Title => asc ? query.OrderBy(r => r.Title) : query.OrderByDescending(r => r.Title),
            RequestSortField.OrganizationName => asc ? query.OrderBy(r => r.OrganizationName) : query.OrderByDescending(r => r.OrganizationName),
            RequestSortField.Priority => asc ? query.OrderBy(r => r.Priority) : query.OrderByDescending(r => r.Priority),
            RequestSortField.Status => asc ? query.OrderBy(r => r.Status) : query.OrderByDescending(r => r.Status),
            _ => asc ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
        };
        return ordered.ThenBy(r => r.Id);
    }
    public async Task<UpdateStatusResult> UpdateStatusAsync(
    int id, RequestStatus newStatus, byte[] expectedVersion, string changedBy, CancellationToken ct)
    {

        var request = await _db.Requests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null)
            return new(UpdateOutcome.NotFound, Error: $"Request {id} was not found.");


        if (!request.RowVersion.SequenceEqual(expectedVersion)) {
            _logger.LogWarning("Concurrency conflict (stale version) on request {Id}", id);
            return new(UpdateOutcome.Conflict,
                Error: "The request was modified by someone else. Reload and try again.");
}
       
        if (!RequestStatusTransitions.CanTransition(request.Status, newStatus))
        {
            var allowed = RequestStatusTransitions.GetAllowed(request.Status);
            var allowedText = allowed.Count == 0 ? "none" : string.Join(", ", allowed);
            return new(UpdateOutcome.InvalidTransition,
                Error: $"Cannot change status from {request.Status} to {newStatus}. Allowed: {allowedText}.");
        }

        
        var previous = request.Status;
        var now = DateTime.UtcNow;

        request.Status = newStatus;
        request.UpdatedAt = now;

        _db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            RequestId = id,
            PreviousStatus = previous,
            NewStatus = newStatus,
            ChangedAt = now,
            ChangedBy = changedBy
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {   
            _db.ChangeTracker.Clear();
            _logger.LogWarning("Concurrency conflict (DB level) on request {Id}", id);
            return new(UpdateOutcome.Conflict,
                Error: "The request was modified by someone else. Reload and try again.");
          
        }

        _logger.LogInformation("Request {Id} status changed {Previous} -> {New} by {ChangedBy}",id, previous, newStatus, changedBy);
        return new(UpdateOutcome.Success, request.ToDto());
    }
    public async Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(int id, CancellationToken ct)
    {
        var exists = await _db.Requests.AnyAsync(r => r.Id == id, ct);
        if (!exists) return null;

        return await _db.RequestStatusHistories
            .AsNoTracking()
            .Where(h => h.RequestId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new RequestStatusHistoryDto(
                h.Id, h.RequestId, h.PreviousStatus, h.NewStatus, h.ChangedAt, h.ChangedBy))
            .ToListAsync(ct);
    }

    public async Task<RequestSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
       
        var statusCounts = await _db.Requests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var priorityCounts = await _db.Requests
            .AsNoTracking()
            .GroupBy(r => r.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        
        var byStatus = Enum.GetValues<RequestStatus>()
            .Select(s => new StatusCountDto(s, statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0))
            .ToList();

        var byPriority = Enum.GetValues<RequestPriority>()
            .Select(p => new PriorityCountDto(p, priorityCounts.FirstOrDefault(x => x.Priority == p)?.Count ?? 0))
            .ToList();

        return new RequestSummaryDto(byStatus.Sum(x => x.Count), byStatus, byPriority);
    }
}