namespace dot_net_api.Entities;

public class QueueTicket
{
    public long Id { get; set; }
    public int CycleNo { get; set; }
    public int Seq { get; set; }
    public string QueueNo { get; set; } = "";
    public DateTimeOffset IssuedAt { get; set; }
}
