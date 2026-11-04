namespace Common.Models;

public static class RequestStatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = [RequestStatus.InProgress, RequestStatus.Waiting],
            [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
            [RequestStatus.Waiting] = [RequestStatus.InProgress, RequestStatus.Completed],
            [RequestStatus.Completed] = []
        };

    public static bool CanTransition(RequestStatus from, RequestStatus to)
        => Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyCollection<RequestStatus> GetAllowed(RequestStatus from)
        => Allowed.TryGetValue(from, out var targets) ? targets : [];
}