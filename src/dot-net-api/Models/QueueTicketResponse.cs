namespace dot_net_api.Models;

public record QueueTicketResponse(
    long Id,
    int CycleNo,
    int Seq,
    string QueueNo,
    DateTimeOffset IssuedAt);
