using dot_net_api.Entities;

namespace dot_net_api.Repositories;

/// <summary>
/// Data access for the queue tables. Contains no business rules;
/// those live in <see cref="Services.IQueueService"/>.
/// </summary>
public interface IQueueRepository
{
    /// <summary>Loads the single counter row and locks it until the current transaction ends.</summary>
    Task<QueueCounter> GetCounterForUpdateAsync(CancellationToken ct = default);

    /// <summary>Reads the counter without locking it (for read-only checks). Null if the row doesn't exist yet.</summary>
    Task<QueueCounter?> GetCounterAsync(CancellationToken ct = default);

    Task<QueueTicket?> GetLatestTicketAsync(CancellationToken ct = default);

    void AddTicket(QueueTicket ticket);

    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Runs <paramref name="action"/> inside a database transaction (commit on success, rollback on exception).</summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
