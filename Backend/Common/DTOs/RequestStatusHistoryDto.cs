using Common.Models;

namespace Common.DTOs;

public record RequestStatusHistoryDto(
    long Id,
    int RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy
    );