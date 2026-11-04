using Common.Models;

namespace Common.DTOs;

public record StatusCountDto(RequestStatus Status, int Count);

public record PriorityCountDto(RequestPriority Priority, int Count);

public record RequestSummaryDto(
    int Total,
    IReadOnlyList<StatusCountDto> ByStatus,
    IReadOnlyList<PriorityCountDto> ByPriority);