namespace Common.DTOs;

public enum UpdateOutcome { Success, NotFound, Conflict, InvalidTransition }

public record UpdateStatusResult(UpdateOutcome Outcome, RequestDto? Request = null, string? Error = null);