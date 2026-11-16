namespace dot_net_api.Entities;

public class QueueCounter
{
    public short Id { get; set; } = 1;
    public int CycleNo { get; set; } = 1;
    public int LastSeq { get; set; } = -1;
    public DateTimeOffset UpdatedAt { get; set; }
}
