using dot_net_api.Data;
using dot_net_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_api.Repositories;

public class QueueRepository(QueueDbContext db) : IQueueRepository
{
    public async Task<QueueCounter> GetCounterForUpdateAsync(CancellationToken ct = default)
    {
        // First enqueue ever (or the seed row is missing): create the counter so the first ticket is A0 in cycle 1.
        // ON CONFLICT DO NOTHING makes this a no-op once the row exists, and safe if two requests race here.
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO queue_counter (id, cycle_no, last_seq, updated_at) " +
            "VALUES (1, 1, -1, now()) ON CONFLICT (id) DO NOTHING",
            ct);

        // FOR UPDATE blocks other requests from reading the counter until we commit,
        // so two callers can never be handed the same sequence number.
        return await db.QueueCounters
            .FromSqlRaw("SELECT * FROM queue_counter WHERE id = 1 FOR UPDATE")
            .SingleAsync(ct);
    }

    public Task<QueueCounter?> GetCounterAsync(CancellationToken ct = default) =>
        db.QueueCounters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == 1, ct);

    public Task<QueueTicket?> GetLatestTicketAsync(CancellationToken ct = default) =>
        db.QueueTickets
            .AsNoTracking()
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(ct);

    public void AddTicket(QueueTicket ticket) => db.QueueTickets.Add(ticket);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        // Use the execution strategy so this stays correct if connection retries are enabled later.
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var result = await action();
            await tx.CommitAsync(ct);
            return result;
        });
    }
}
