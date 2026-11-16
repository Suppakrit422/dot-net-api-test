namespace dot_net_api.Models;

/// <summary>The cycle the queue is on after a reset. The next ticket issued will be A0 in this cycle.</summary>
public record QueueCycleResponse(
    int CycleNo,
    DateTimeOffset StartedAt);
