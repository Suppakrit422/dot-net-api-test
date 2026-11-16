using dot_net_api.Models;

namespace dot_net_api.Services;

public interface IQueueService
{
    Task<QueueTicketResponse> IssueTicketAsync(CancellationToken ct = default);
    Task<QueueTicketResponse?> GetCurrentAsync(CancellationToken ct = default);
    Task<QueueCycleResponse?> ResetAsync(CancellationToken ct = default);

}
