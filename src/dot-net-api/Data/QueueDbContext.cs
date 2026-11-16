using dot_net_api.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_api.Data;

public class QueueDbContext(DbContextOptions<QueueDbContext> options) : DbContext(options)
{
    public DbSet<QueueCounter> QueueCounters => Set<QueueCounter>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<QueueCounter>(e =>
        {
            e.ToTable("queue_counter", t =>
            {
                t.HasCheckConstraint("ck_queue_counter_single_row", "id = 1");
                t.HasCheckConstraint("ck_queue_counter_last_seq", "last_seq BETWEEN -1 AND 259");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.CycleNo).HasColumnName("cycle_no");
            e.Property(x => x.LastSeq).HasColumnName("last_seq");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            e.HasData(new QueueCounter { Id = 1, CycleNo = 1, LastSeq = -1,
                                         UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
        });

        b.Entity<QueueTicket>(e =>
        {
            e.ToTable("queue_ticket", t =>
                t.HasCheckConstraint("ck_queue_ticket_seq", "seq BETWEEN 0 AND 259"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            e.Property(x => x.CycleNo).HasColumnName("cycle_no");
            e.Property(x => x.Seq).HasColumnName("seq");
            e.Property(x => x.QueueNo).HasColumnName("queue_no").HasMaxLength(2);
            e.Property(x => x.IssuedAt).HasColumnName("issued_at").HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.CycleNo, x.Seq }).IsUnique();
        });
    }
}