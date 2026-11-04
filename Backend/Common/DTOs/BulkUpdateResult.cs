namespace Common.DTOs;

public record BulkItemResult(
    int Id,
    UpdateOutcome Outcome,
    RequestDto? Request = null,
    string? Error = null);

public record BulkUpdateResult(
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<BulkItemResult> Results
    );