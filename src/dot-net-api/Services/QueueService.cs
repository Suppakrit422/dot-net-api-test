using dot_net_api.Entities;
using dot_net_api.Models;
using dot_net_api.Repositories;

namespace dot_net_api.Services;

public class QueueService(IQueueRepository repository, TimeProvider clock) : IQueueService
{
    /// <summary>Highest sequence in a cycle (matches the DB check constraint: seq BETWEEN 0 AND 259).</summary>
    public const int MaxSeq = 259;

    public Task<QueueTicketResponse> IssueTicketAsync(CancellationToken ct = default) =>
        repository.ExecuteInTransactionAsync(async () =>
        {
            var counter = await repository.GetCounterForUpdateAsync(ct);

            // Roll over to a new cycle after the last number (Z9).
            if (counter.LastSeq >= MaxSeq)
            {
                counter.CycleNo++;
                counter.LastSeq = -1;
            }

            counter.LastSeq++;
            var now = clock.GetUtcNow();
            counter.UpdatedAt = now;

            var ticket = new QueueTicket
            {
                CycleNo = counter.CycleNo,
                Seq = counter.LastSeq,
                QueueNo = FormatQueueNo(counter.LastSeq),
                IssuedAt = now
            };
            repository.AddTicket(ticket);
            await repository.SaveChangesAsync(ct);

            return ToResponse(ticket);
        }, ct);

    public async Task<QueueTicketResponse?> GetCurrentAsync(CancellationToken ct = default)
    {
        var ticket = await repository.GetLatestTicketAsync(ct);
        if (ticket is null) return null;

        // After a reset the newest ticket belongs to the previous cycle; don't show it as current.
        var counter = await repository.GetCounterAsync(ct);
        return counter is not null && ticket.CycleNo == counter.CycleNo ? ToResponse(ticket) : null;
    }

    public Task<QueueCycleResponse?> ResetAsync(CancellationToken ct = default) =>
        repository.ExecuteInTransactionAsync(async () =>
        {
            // Same row lock as IssueTicketAsync, so a reset and an enqueue never interleave.
            var counter = await repository.GetCounterForUpdateAsync(ct);

            // Nothing issued in this cycle yet: refuse, so repeated resets don't create empty cycles.
            if (counter.LastSeq == -1) return null;

            counter.CycleNo++;
            counter.LastSeq = -1;
            counter.UpdatedAt = clock.GetUtcNow();
            await repository.SaveChangesAsync(ct);

            return new QueueCycleResponse(counter.CycleNo, counter.UpdatedAt);
        }, ct);

    public static string FormatQueueNo(int seq)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seq);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(seq, MaxSeq);
        return $"{(char)('A' + seq / 10)}{seq % 10}";
    }

    private static QueueTicketResponse ToResponse(QueueTicket t) =>
        new(t.Id, t.CycleNo, t.Seq, t.QueueNo, t.IssuedAt);

}
