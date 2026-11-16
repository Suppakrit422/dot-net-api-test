using dot_net_api.Entities;
using dot_net_api.Repositories;
using dot_net_api.Services;

namespace dot_net_api.Tests;

public class QueueServiceTests
{
    [Theory]
    [InlineData(0, "A0")]
    [InlineData(9, "A9")]
    [InlineData(10, "B0")]
    [InlineData(259, "Z9")]
    public void FormatQueueNo_maps_seq_to_letter_and_digit(int seq, string expected) =>
        Assert.Equal(expected, QueueService.FormatQueueNo(seq));

    [Theory]
    [InlineData(-1)]
    [InlineData(260)]
    public void FormatQueueNo_rejects_out_of_range(int seq) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => QueueService.FormatQueueNo(seq));

    [Fact]
    public async Task IssueTicket_first_ticket_is_A0()
    {
        var repo = new FakeQueueRepository(new QueueCounter { CycleNo = 1, LastSeq = -1 });
        var service = new QueueService(repo, TimeProvider.System);

        var ticket = await service.IssueTicketAsync();

        Assert.Equal("A0", ticket.QueueNo);
        Assert.Equal(0, ticket.Seq);
        Assert.Equal(1, ticket.CycleNo);
        Assert.Equal(0, repo.Counter.LastSeq);
        Assert.Single(repo.Tickets);
        Assert.Equal(1, repo.TransactionCount);
    }

    [Fact]
    public async Task IssueTicket_after_Z9_starts_new_cycle_at_A0()
    {
        var repo = new FakeQueueRepository(new QueueCounter { CycleNo = 3, LastSeq = QueueService.MaxSeq });
        var service = new QueueService(repo, TimeProvider.System);

        var ticket = await service.IssueTicketAsync();

        Assert.Equal("A0", ticket.QueueNo);
        Assert.Equal(4, ticket.CycleNo);
        Assert.Equal(4, repo.Counter.CycleNo);
        Assert.Equal(0, repo.Counter.LastSeq);
    }

    [Fact]
    public async Task GetCurrent_returns_null_when_no_tickets() =>
        Assert.Null(await new QueueService(new FakeQueueRepository(new QueueCounter()), TimeProvider.System).GetCurrentAsync());

    [Fact]
    public async Task Reset_starts_new_cycle_and_next_ticket_is_A0()
    {
        var repo = new FakeQueueRepository(new QueueCounter { CycleNo = 3, LastSeq = 27 }); // last issued C7
        var service = new QueueService(repo, TimeProvider.System);

        var cycle = await service.ResetAsync();

        Assert.NotNull(cycle);
        Assert.Equal(4, cycle.CycleNo);
        Assert.Equal(4, repo.Counter.CycleNo);
        Assert.Equal(-1, repo.Counter.LastSeq);
        Assert.Equal(1, repo.TransactionCount);

        var next = await service.IssueTicketAsync();
        Assert.Equal("A0", next.QueueNo);
        Assert.Equal(4, next.CycleNo);
    }

    [Fact]
    public async Task Reset_is_refused_when_current_cycle_has_no_tickets()
    {
        var repo = new FakeQueueRepository(new QueueCounter { CycleNo = 2, LastSeq = -1 });
        var service = new QueueService(repo, TimeProvider.System);

        Assert.Null(await service.ResetAsync());
        Assert.Equal(2, repo.Counter.CycleNo); // unchanged
        Assert.Equal(-1, repo.Counter.LastSeq);
    }

    [Fact]
    public async Task GetCurrent_returns_null_after_reset_until_next_ticket()
    {
        var repo = new FakeQueueRepository(new QueueCounter { CycleNo = 1, LastSeq = -1 });
        var service = new QueueService(repo, TimeProvider.System);
        await service.IssueTicketAsync();                                // A0, cycle 1

        Assert.Equal("A0", (await service.GetCurrentAsync())?.QueueNo);

        await service.ResetAsync();
        Assert.Null(await service.GetCurrentAsync());                    // old cycle's ticket hidden

        await service.IssueTicketAsync();                                // A0, cycle 2
        var current = await service.GetCurrentAsync();
        Assert.Equal(2, current?.CycleNo);
        Assert.Equal("A0", current?.QueueNo);
    }

    /// <summary>In-memory stand-in for the real repository, the main payoff of the repository pattern.</summary>
    private sealed class FakeQueueRepository(QueueCounter counter) : IQueueRepository
    {
        public QueueCounter Counter { get; } = counter;
        public List<QueueTicket> Tickets { get; } = [];
        public int TransactionCount { get; private set; }

        public Task<QueueCounter> GetCounterForUpdateAsync(CancellationToken ct = default) => Task.FromResult(Counter);

        public Task<QueueCounter?> GetCounterAsync(CancellationToken ct = default) => Task.FromResult<QueueCounter?>(Counter);

        public Task<QueueTicket?> GetLatestTicketAsync(CancellationToken ct = default) =>
            Task.FromResult(Tickets.OrderByDescending(t => t.Id).FirstOrDefault());

        public Task<QueueTicket?> GetTicketByIdAsync(long id, CancellationToken ct = default) =>
            Task.FromResult(Tickets.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<QueueTicket>> GetTicketsByCycleAsync(int cycleNo, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<QueueTicket>>(Tickets.Where(t => t.CycleNo == cycleNo).OrderBy(t => t.Seq).ToList());

        public void AddTicket(QueueTicket ticket)
        {
            ticket.Id = Tickets.Count + 1;
            Tickets.Add(ticket);
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
        {
            TransactionCount++;
            return action();
        }
    }
}
